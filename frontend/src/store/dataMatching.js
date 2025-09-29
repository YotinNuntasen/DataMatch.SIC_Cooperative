// datamatching.js

import sharepointService from "../services/sharepointService";
import azureService from "../services/azureService";
import { calculateSimilarity } from "../utils/similarity";

function normalizeDateToISOString(value) {
  if (value === undefined || value === null) {
    return null;
  }

  let dateObj;

  if (value instanceof Date) {
    dateObj = value;
  } else {
    const s = String(value).trim();
    if (!s) {
      return null;
    }
    dateObj = new Date(s);
  }

  if (isNaN(dateObj.getTime())) {
    console.warn("Invalid date value, converting to null:", value);
    return null;
  }

  return dateObj.toISOString();
}

// Helper function สำหรับคำนวณรายได้
function calculateRevenue(azureItem) {
  const rawTotalSales =
    azureItem.calculatedRevenue ||
    azureItem.totalSales ||
    azureItem.TotalSales ||
    "0";
  const cleanedTotalSales = parseFloat(
    String(rawTotalSales).replace(/[^0-9.-]+/g, "")
  );
  return cleanedTotalSales || 0;
}

function calculateDateRange(s9DWINEntryDate) {
  if (!s9DWINEntryDate) return null;

  try {
    const documentDate = new Date(s9DWINEntryDate);
    // ตรวจสอบว่าเป็นวันที่ถูกต้องหรือไม่
    if (isNaN(documentDate.getTime())) {
      console.warn(`[Date Range] Invalid s9DWINEntryDate:`, s9DWINEntryDate);
      return null;
    }

    // คำนวณวันเริ่มต้น (ย้อนหลัง 1.5 เดือน)
    const startDate = new Date(documentDate);
    startDate.setMonth(startDate.getMonth() - 1.5);

    // คำนวณวันสิ้นสุด (ไปข้างหน้า 2 ปี)
    const endDate = new Date(documentDate);
    endDate.setFullYear(endDate.getFullYear() + 2);

    return { startDate, endDate };
  } catch (error) {
    console.error(`[Date Range] Error parsing date:`, s9DWINEntryDate, error);
    return null;
  }
}

const state = {
  sharePointData: [],
  azureTableData: [],
  selectedSharePointItem: null,
  similarItems: [],
  matchedGroups: JSON.parse(localStorage.getItem("matchedGroups")) || {},
  showAllSimilar: false,
  loading: false,
  error: null,

  sharePointSortKey: "s9DWINEntryDate",
  sharePointSortDirection: "desc",
  azureSortKey: "similarity",
  azureSortDirection: "desc",
};

const getters = {
  sharePointData: (state) => state.sharePointData || [],
  azureTableData: (state) => state.azureTableData || [],
  selectedSharePointItem: (state) => state.selectedSharePointItem,
  matchedGroupsArray: (state) => Object.values(state.matchedGroups || {}),

  // Enhanced SharePoint data พร้อม revenue calculation
  enhancedSharePointData: (state) => {
    if (!state.sharePointData || !Array.isArray(state.sharePointData)) {
      return [];
    }

    let processedData = state.sharePointData.map((spItem) => {
      const matchedGroup = state.matchedGroups[spItem.id];
      let totalRevenue = 0;
      let matchedRecordsCount = 0;

      if (matchedGroup && matchedGroup.matchedCustomers) {
        matchedGroup.matchedCustomers.forEach((azureItem) => {
          totalRevenue += calculateRevenue(azureItem);
          matchedRecordsCount++;
        });
      }

      return {
        ...spItem,
        calculatedRevenue: totalRevenue,
        matchedRecordsCount: matchedRecordsCount,
        hasMatches: matchedRecordsCount > 0,
      };
    });

    const sortKey = state.sharePointSortKey;
    const sortDirection = state.sharePointSortDirection;

    if (sortKey) {
      processedData.sort((a, b) => {
        let valA = a[sortKey];
        let valB = b[sortKey];

        // Handle specific sorting cases for dates and currencies
        if (sortKey.includes("Date")) {
          valA = valA ? new Date(valA).getTime() : 0;
          valB = valB ? new Date(valB).getTime() : 0;
        } else if (sortKey === "calculatedRevenue") {
          valA = parseFloat(valA || 0);
          valB = parseFloat(valB || 0);
        } else {
          valA = String(valA || "").toLowerCase();
          valB = String(valB || "").toLowerCase();
        }

        if (valA < valB) return sortDirection === "asc" ? -1 : 1;
        if (valA > valB) return sortDirection === "asc" ? 1 : -1;
        return 0;
      });
    }

    return processedData;
  },

  displaySharePointData: (state, getters) => {
    return getters.enhancedSharePointData;
  },

  sharePointSortConfig: (state) => ({
    key: state.sharePointSortKey,
    direction: state.sharePointSortDirection,
  }),

  flatMatchedPairs: (state) => {
    return Object.values(state.matchedGroups).flatMap((group) =>
      group.matchedCustomers.map((customer) => ({
        id: `${group.sharePointItem.id}-${customer.RowKey}`,
        sharepoint: group.sharePointItem,
        azure: customer,
        calculatedRevenue: customer.calculatedRevenue,
      }))
    );
  },

  totalRevenue: (state, getters) => {
    return getters.flatMatchedPairs.reduce((total, pair) => {
      return total + (pair.calculatedRevenue || 0);
    }, 0);
  },

  revenueStatistics: (state, getters) => {
    const pairs = getters.flatMatchedPairs;
    if (pairs.length === 0) {
      return {
        total: 0,
        average: 0,
        highest: 0,
        lowest: 0,
        count: 0,
      };
    }

    const revenues = pairs.map((pair) => pair.calculatedRevenue || 0);
    const total = revenues.reduce((sum, rev) => sum + rev, 0);

    return {
      total: total,
      average: total / revenues.length,
      highest: Math.max(...revenues),
      lowest: Math.min(...revenues),
      count: revenues.length,
    };
  },

  similarItems: (state) => {
    if (!state.selectedSharePointItem) return [];

    const currentGroup = state.matchedGroups[state.selectedSharePointItem.id];
    const matchedAzureRowKeys = currentGroup
      ? currentGroup.matchedCustomers.map((m) => m.RowKey)
      : [];

    let filteredItems = (state.similarItems || []).filter(
      (item) => !matchedAzureRowKeys.includes(item.RowKey || item.rowKey) // เพิ่ม fallback สำหรับ case sensitivity
    );

    const sortKey = state.azureSortKey;
    const sortDirection = state.azureSortDirection;

    if (sortKey) {
      filteredItems.sort((a, b) => {
        let valA, valB;

        if (sortKey === "documentDate") {
          valA = a[sortKey] ? new Date(a[sortKey]).getTime() : 0;
          valB = b[sortKey] ? new Date(b[sortKey]).getTime() : 0;
        } else if (sortKey === "similarity") {
          valA = parseFloat(a[sortKey] || 0);
          valB = parseFloat(b[sortKey] || 0);
        } else {
          valA = String(a[sortKey] || "").toLowerCase();
          valB = String(b[sortKey] || "").toLowerCase();
        }

        if (valA < valB) return sortDirection === "asc" ? -1 : 1;
        if (valA > valB) return sortDirection === "asc" ? 1 : -1;
        return 0;
      });
    }

    return filteredItems;
  },

  topSimilarItems: (state, getters) => (getters.similarItems || []).slice(0, 8),

  showAllSimilar: (state) => state.showAllSimilar,
  loading: (state) => state.loading,
  error: (state) => state.error,

  unmatchedSharePointData: (state) => {
    const matchedSharePointIds = Object.keys(state.matchedGroups || {});
    return (state.sharePointData || []).filter(
      (item) => !matchedSharePointIds.includes(item.id)
    );
  },

  matchedCount: (state, getters) => getters.flatMatchedPairs.length,

  exportData: (state, getters) => getters.flatMatchedPairs,

  matchingStatistics: (state, getters) => {
    const enhanced = getters.enhancedSharePointData;
    return {
      totalOpportunities: enhanced.length,
      matchedOpportunities: enhanced.filter((item) => item.hasMatches).length,
      unmatchedOpportunities: enhanced.filter((item) => !item.hasMatches)
        .length,
      totalMatchedRecords: enhanced.reduce(
        (sum, item) => sum + item.matchedRecordsCount,
        0
      ),
      averageMatchesPerOpportunity:
        enhanced.length > 0
          ? enhanced.reduce((sum, item) => sum + item.matchedRecordsCount, 0) /
            enhanced.length
          : 0,
    };
  },

  azureSortConfig: (state) => ({
    key: state.azureSortKey,
    direction: state.azureSortDirection,
  }),
};

const mutations = {
  SET_SHAREPOINT_DATA(state, data) {
    state.sharePointData = Array.isArray(data) ? data : [];
  },
  SET_AZURE_TABLE_DATA(state, data) {
    state.azureTableData = Array.isArray(data) ? data : [];
  },
  SET_SELECTED_SHAREPOINT_ITEM(state, item) {
    state.selectedSharePointItem = item;
  },
  SET_SIMILAR_ITEMS(state, items) {
    state.similarItems = Array.isArray(items) ? items : [];
  },
  SET_SHOW_ALL_SIMILAR(state, show) {
    state.showAllSimilar = show;
  },
  SET_LOADING(state, loading) {
    state.loading = loading;
  },
  SET_ERROR(state, error) {
    state.error = error;
  },
  CLEAR_ERROR(state) {
    state.error = null;
  },

  SET_SHAREPOINT_SORT(state, { key, direction }) {
    state.sharePointSortKey = key;
    state.sharePointSortDirection = direction;
  },
  SET_AZURE_SORT(state, { key, direction }) {
    state.azureSortKey = key;
    state.azureSortDirection = direction;
  },

  ADD_MATCH(state, { sharePointItem, azureItem, similarity }) {
    const spId = sharePointItem.id;
    if (!state.matchedGroups[spId]) {
      state.matchedGroups[spId] = {
        sharePointItem: sharePointItem,
        matchedCustomers: [],
      };
    }

    const itemRevenue = calculateRevenue(azureItem);
    const nowISOString = new Date().toISOString();

    const mergedPersonDocument = {
      PartitionKey: azureItem.PartitionKey || "MergedCustomer",
      RowKey: azureItem.RowKey || azureItem.rowKey,
      custShortDimName:
        azureItem.custShortDimName || sharePointItem.customerName || "",
      postingDate: normalizeDateToISOString(azureItem.postingDate),
      prefixdocumentNo: azureItem.prefixdocumentNo || "",
      selltoCustName_SalesHeader: azureItem.selltoCustName_SalesHeader || "",
      systemRowVersion: azureItem.systemRowVersion || "",
      documentDate: normalizeDateToISOString(
        azureItem.documentDate || sharePointItem.s9DWINEntryDate
      ),
      documentNo: azureItem.documentNo || "",
      itemReferenceNo: azureItem.PCode || sharePointItem.productCode || "",
      lineNo: azureItem.lineNo || 0,
      no: azureItem.no || "",
      quantity: azureItem.quantity || 0,
      sellToCustomerNo: azureItem.sellToCustomerNo || "",
      shipmentNo: azureItem.shipmentNo || "",
      sodocumentNo: azureItem.sodocumentNo || "",
      srodocumentNo: azureItem.SrodocumentNo || "",
      description:
        azureItem.description || sharePointItem.productInterest || "",
      unitOfMeasure: azureItem.UnitOfMeasure || "",
      unitPrice: azureItem.unitPrice || 0,
      lineDiscount: azureItem.LineDiscount || 0,
      lineAmount: azureItem.lineAmount || 0,
      currencyCode: azureItem.currencyCode || "",
      currencyRate: azureItem.currencyRate || 0,
      salesPerUnit: azureItem.SalesPerUnit || 0,
      totalSales: azureItem.totalSales || azureItem.TotalSales || 0,
      podocumentNo: azureItem.PodocumentNo || "",
      custAppDimName: azureItem.custAppDimName || "",
      prodChipNameDimName:
        azureItem.prodChipNameDimName || sharePointItem.productGroup || "",
      regionDimName3: azureItem.regionDimName3 || "",
      salespersonDimName:
        azureItem.SalesName || sharePointItem.customerNameSalePersonCode || "",
      "opportunity ID": sharePointItem.opportunityId || "",
      calculatedRevenue: itemRevenue,
      revenueCalculationTimestamp: nowISOString,
      // created: normalizeDateToISOString(sharePointItem.created),
      modified: nowISOString,
      matchedDate: nowISOString,
      id: azureItem.RowKey || azureItem.id,
    };

    if (!mergedPersonDocument.RowKey || mergedPersonDocument.RowKey === "-") {
      console.error(
        "Generated PersonDocument has invalid RowKey:",
        mergedPersonDocument
      );
      alert("Error: Cannot match item due to invalid RowKey generation.");
      return;
    }

    if (
      !state.matchedGroups[spId].matchedCustomers.some(
        (c) => c.RowKey === mergedPersonDocument.RowKey
      )
    ) {
      state.matchedGroups[spId].matchedCustomers.push({
        ...mergedPersonDocument,
        similarity: similarity,
      });

      console.log(
        `✅ Added match: SP Item ${spId} with Azure Item (RowKey: ${
          mergedPersonDocument.RowKey
        }), Revenue: $${itemRevenue.toFixed(2)}`
      );
    } else {
      console.warn(
        `⚠️ Attempted to add duplicate match (RowKey: ${mergedPersonDocument.RowKey}) for SP Item ${spId}.`
      );
    }
    localStorage.setItem("matchedGroups", JSON.stringify(state.matchedGroups));
    console.log("MatchedGroups after ADD_MATCH:", state.matchedGroups);
  },

  // Mutation สำหรับการ Unmatch
  REMOVE_MATCH(state, { sharepointId, azureRowKey }) {
    if (state.matchedGroups[sharepointId]) {
      const removedItems = state.matchedGroups[
        sharepointId
      ].matchedCustomers.filter((c) => c.RowKey === azureRowKey);
      if (removedItems.length > 0) {
        const removedRevenue = removedItems.reduce(
          (sum, item) => sum + (item.calculatedRevenue || 0),
          0
        ); // ใช้ item.calculatedRevenue
        console.log(
          `Removed match: Revenue lost: $${removedRevenue.toFixed(2)}`
        );
      }

      state.matchedGroups[sharepointId].matchedCustomers = state.matchedGroups[
        sharepointId
      ].matchedCustomers.filter((c) => c.RowKey !== azureRowKey); // แก้ไขตรงนี้ให้ Filter ด้วย c.RowKey

      if (state.matchedGroups[sharepointId].matchedCustomers.length === 0) {
        delete state.matchedGroups[sharepointId];
      }
    }
    localStorage.setItem("matchedGroups", JSON.stringify(state.matchedGroups));
    console.log("MatchedGroups after REMOVE_MATCH:", state.matchedGroups);
  },

  CLEAR_ALL_MATCHES(state) {
    state.matchedGroups = {};
    localStorage.removeItem("matchedGroups");
    console.log("All matches cleared");
  },

  // --- ปรับปรุง RESET_STATE ให้รีเซ็ต sorting ด้วย ---
  RESET_STATE(state) {
    state.sharePointData = [];
    state.azureTableData = [];
    state.selectedSharePointItem = null;
    state.similarItems = [];
    state.matchedGroups = {}; // Clear matchedGroups on reset
    state.showAllSimilar = false;
    state.loading = false;
    state.error = null;
    // Reset sorting states
    state.sharePointSortKey = "s9DWINEntryDate";
    state.sharePointSortDirection = "desc";
    state.azureSortKey = "similarity";
    state.azureSortDirection = "desc";
    localStorage.removeItem("matchedGroups");
  },
};

const actions = {
  async loadSharePointData({ commit }) {
    commit("SET_LOADING", true);
    commit("SET_ERROR", null);
    try {
      const rawData = await sharepointService.getSharePointData();

      if (!Array.isArray(rawData)) {
        throw new Error(
          "Received data is not an array from SharePoint service."
        );
      }

      const filteredData = rawData.filter((item) => {
        return item.pipelineStage == "Design-Win";
      });

      console.log(
        `SharePoint: Fetched ${rawData.length} records, filtered down to ${filteredData.length} records with a pipelineStage.`
      );

      commit("SET_SHAREPOINT_DATA", filteredData);
    } catch (error) {
      console.error("Failed to load and filter SharePoint Data:", error);
      commit("SET_ERROR", `SharePoint Load Failed: ${error.message}`);
      commit("SET_SHAREPOINT_DATA", []);
    } finally {
      commit("SET_LOADING", false);
    }
  },

  async sendMatchedGroupsToBackend({ state }) {
    const sanitizedRecords = Object.values(state.matchedGroups).flatMap(
      (group) =>
        group.matchedCustomers.map((customer) => ({
          PartitionKey: customer.PartitionKey,
          RowKey: customer.RowKey,
          OpportunityId: customer.OpportunityId,
          CustShortDimName: customer.custShortDimName,
          PostingDate: normalizeDateToISOString(customer.postingDate),
          PrefixdocumentNo: customer.prefixdocumentNo,
          SelltoCustName_SalesHeader: customer.selltoCustName_SalesHeader,
          SystemRowVersion: customer.systemRowVersion,
          DocumentDate: normalizeDateToISOString(customer.documentDate),
          documentNo: customer.documentNo,
          itemReferenceNo: customer.itemReferenceNo,
          lineNo: customer.lineNo,
          no: customer.no,
          quantity: customer.quantity,
          sellToCustomerNo: customer.sellToCustomerNo,
          shipmentNo: customer.shipmentNo,
          sodocumentNo: customer.sodocumentNo,
          SrodocumentNo: customer.srodocumentNo,
          description: customer.description,
          UnitOfMeasure: customer.unitOfMeasure,
          unitPrice: customer.unitPrice,
          LineDiscount: customer.lineDiscount,
          lineAmount: customer.lineAmount,
          currencyCode: customer.currencyCode,
          CurrencyRate: customer.currencyRate,
          SalesPerUnit: customer.salesPerUnit,
          TotalSales: customer.totalSales,
          PodocumentNo: customer.podocumentNo,
          CustAppDimName: customer.custAppDimName,
          ProdChipNameDimName: customer.prodChipNameDimName,
          RegionDimName3: customer.regionDimName3,
          SalespersonDimName: customer.salespersonDimName,

          SharePointListName: customer.sharePointListName,
          Modified: normalizeDateToISOString(customer.modified),
          CreatedBy: customer.createdBy || "DataMatchPortal",
          MatchedDate: normalizeDateToISOString(customer.matchedDate),
          Status: customer.status || "Active",
          LastContactDate: normalizeDateToISOString(customer.lastContactDate),
        }))
    );

    const payload = { records: sanitizedRecords };

    return await fetch("/api/customer-data/merged", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
  },

  async loadAzureTableData({ commit }) {
    commit("SET_LOADING", true);
    commit("SET_ERROR", null);
    try {
      const data = await azureService.getAzureTableData();
      commit("SET_AZURE_TABLE_DATA", data);
    } catch (error) {
      console.error("Failed to load Azure Table Data:", error);
      commit("SET_ERROR", `Azure Table Load Failed: ${error.message}`);
      commit("SET_AZURE_TABLE_DATA", []);
    } finally {
      commit("SET_LOADING", false);
    }
  },

  selectSharePointItem({ commit, state }, item) {
    if (
      state.selectedSharePointItem &&
      state.selectedSharePointItem.id === item.id
    ) {
      commit("SET_SELECTED_SHAREPOINT_ITEM", null);
      commit("SET_SIMILAR_ITEMS", []);
      return;
    }

    commit("SET_SELECTED_SHAREPOINT_ITEM", item);

    const azureDataToProcess = state.azureTableData;

    // คำนวณ similarity และ potentialRevenue
    const similarities = azureDataToProcess
      .map((azureItem) => ({
        ...azureItem,
        similarity: calculateSimilarity(item, azureItem),
        potentialRevenue: calculateRevenue(azureItem),
      }))
      .filter((i) => i.similarity > 0);

    commit("SET_SIMILAR_ITEMS", similarities);
    commit("SET_SHOW_ALL_SIMILAR", false);
  },

  matchItems({ commit }, { sharePointItem, azureItem, similarity }) {
    commit("ADD_MATCH", { sharePointItem, azureItem, similarity });
  },

  unmatchItems({ commit }, { sharepointId, azureRowKey }) {
    // แก้ไขเป็น azureRowKey
    commit("REMOVE_MATCH", { sharepointId, azureRowKey }); // ส่ง azureRowKey ไปยัง mutation
  },

  clearAllMatches({ commit }) {
    commit("CLEAR_ALL_MATCHES");
  },

  resetState({ commit }) {
    commit("RESET_STATE"); // ใช้ RESET_STATE ใหม่
  },

  showAllSimilarItems({ commit }) {
    commit("SET_SHOW_ALL_SIMILAR", true);
  },

  hideAllSimilarItems({ commit }) {
    commit("SET_SHOW_ALL_SIMILAR", false);
  },

  // --- เพิ่ม Action สำหรับ Sorting SharePoint Data ---
  setSharePointSort({ commit, state }, { key }) {
    let direction = "desc";

    if (state.sharePointSortKey === key) {
      direction = state.sharePointSortDirection === "desc" ? "asc" : "desc";
    } else if (key === "customerName" || key === "opportunityName") {
      direction = "asc";
    }
    commit("SET_SHAREPOINT_SORT", { key, direction });
  },
  // --- เพิ่ม Action สำหรับ Sorting Azure Data ---
  setAzureSort({ commit, state }, { key }) {
    let direction = "desc";
    if (state.azureSortKey === key) {
      direction = state.azureSortDirection === "desc" ? "asc" : "desc";
    } else if (key === "customerName") {
      direction = "asc";
    }
    commit("SET_AZURE_SORT", { key, direction });
  },
};

export default {
  namespaced: true,
  state,
  getters,
  mutations,
  actions,
};
