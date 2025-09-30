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
  previouslyMatchedData: [], // ➡️ เก็บข้อมูล PersonDocument ที่โหลดมาจาก backend
  selectedSharePointItem: null,
  similarItems: [],
  matchedGroups: {}, // ➡️ ไม่ต้องโหลดจาก localStorage โดยตรงอีก เพราะจะ reconstruct จาก previouslyMatchedData
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
          // ➡️ ปรับปรุงการเปรียบเทียบ Date
          valA = valA ? new Date(valA).getTime() : -Infinity;
          valB = valB ? new Date(valB).getTime() : -Infinity;
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
        // ➡️ id ควรมาจาก PersonDocument.RowKey ที่บันทึกไว้
        id: customer.RowKey,
        sharepoint: group.sharePointItem,
        azure: customer, // ทั้ง PersonDocument ถูกใช้เป็น azure
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
    // ➡️ Azure items ที่จับคู่แล้วจะถูกเก็บใน previouslyMatchedData และถูกกรองออกจาก similarItems
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
          // ➡️ ปรับปรุงการเปรียบเทียบ Date
          valA = a[sortKey] ? new Date(a[sortKey]).getTime() : -Infinity;
          valB = b[sortKey] ? new Date(b[sortKey]).getTime() : -Infinity;
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

  RECONSTRUCT_MATCHED_GROUPS(state) {
    const newMatchedGroups = {};
    const debugInfo = {
      totalMergedRecords: 0,
      successfulMatches: 0,
      failedMatches: 0,
      missingOpportunityIds: [],
    };

    if (!Array.isArray(state.previouslyMatchedData)) {
      console.warn("previouslyMatchedData is not an array");
      state.previouslyMatchedData = [];
      return;
    }

    debugInfo.totalMergedRecords = state.previouslyMatchedData.length;

    console.log("🔄 RECONSTRUCT_MATCHED_GROUPS - Starting...");
    console.log("📊 Total merged records:", debugInfo.totalMergedRecords);
    console.log("📊 Total SharePoint items:", state.sharePointData.length);

    state.previouslyMatchedData.forEach((mergedDoc, index) => {
      // ✅ แก้ไข: ใช้ camelCase (opportunityId) แทน PascalCase (OpportunityId)
      const spOpportunityId = mergedDoc.opportunityId;

      if (!spOpportunityId) {
        console.warn(`[${index}] ❌ Missing opportunityId:`, mergedDoc);
        debugInfo.failedMatches++;
        return;
      }

      const sharePointItem = state.sharePointData.find(
        (sp) => sp.opportunityId === spOpportunityId
      );

      if (!sharePointItem) {
        console.warn(
          `[${index}] ⚠️ SharePoint item not found for opportunityId: "${spOpportunityId}"`
        );
        debugInfo.failedMatches++;
        debugInfo.missingOpportunityIds.push(spOpportunityId);
        return;
      }

      // ✅ Found matching SharePoint item
      if (!newMatchedGroups[sharePointItem.id]) {
        newMatchedGroups[sharePointItem.id] = {
          sharePointItem: sharePointItem,
          matchedCustomers: [],
        };
      }

      newMatchedGroups[sharePointItem.id].matchedCustomers.push({
        ...mergedDoc,
        similarity: 100,
        calculatedRevenue: calculateRevenue(mergedDoc),
      });

      debugInfo.successfulMatches++;
    });

    state.matchedGroups = newMatchedGroups;

    console.log("=".repeat(60));
    console.log("📊 RECONSTRUCTION SUMMARY:");
    console.log(
      `✅ Successful: ${debugInfo.successfulMatches}/${debugInfo.totalMergedRecords}`
    );
    console.log(
      `❌ Failed: ${debugInfo.failedMatches}/${debugInfo.totalMergedRecords}`
    );
    console.log(
      `📦 Matched Groups: ${Object.keys(state.matchedGroups).length}`
    );

    if (debugInfo.failedMatches > 0) {
      console.warn(
        "⚠️ Missing opportunities:",
        debugInfo.missingOpportunityIds
      );
    }
    console.log("=".repeat(60));

    localStorage.removeItem("matchedGroups");
  },

  SET_PREVIOUSLY_MATCHED_DATA(state, data) {
    state.previouslyMatchedData = Array.isArray(data) ? data : [];
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
    PartitionKey: sharePointItem.opportunityId,
    RowKey: azureItem.RowKey || azureItem.id,
    
    opportunityId: sharePointItem.opportunityId,
    opportunityName: sharePointItem.title || sharePointItem.opportunityName,
    custShortDimName: azureItem.custShortDimName,
    prefixdocumentNo: azureItem.prefixdocumentNo,
    selltoCustName_SalesHeader: azureItem.selltoCustName_SalesHeader,
    systemRowVersion: azureItem.systemRowVersion,
    documentDate: normalizeDateToISOString(azureItem.documentDate),
    documentNo: azureItem.documentNo,
    itemReferenceNo: azureItem.itemReferenceNo,
    lineNo: azureItem.lineNo,
    no: azureItem.no,
    quantity: azureItem.quantity,
    sellToCustomerNo: azureItem.sellToCustomerNo,
    shipmentNo: azureItem.shipmentNo,
    sodocumentNo: azureItem.sodocumentNo,
    description: azureItem.description,
    unitPrice: azureItem.unitPrice,
    lineDiscount: azureItem.lineDiscount,
    lineAmount: azureItem.lineAmount,
    currencyRate: parseFloat(azureItem.currencyRate || 0),
    salesPerUnit: azureItem.salesPerUnit,
    totalSales: azureItem.totalSales,
    custAppDimName: azureItem.custAppDimName,
    prodChipNameDimName: azureItem.prodChipNameDimName,
    regionDimName3: azureItem.regionDimName3,
    salespersonDimName: azureItem.salespersonDimName,
    
    calculatedRevenue: itemRevenue,
    modified: nowISOString,
    matchedDate: nowISOString,
    status: "Matched",
  };

  if (!mergedPersonDocument.PartitionKey || !mergedPersonDocument.RowKey) {
    console.error("Invalid PartitionKey or RowKey:", mergedPersonDocument);
    alert("Error: Cannot match item due to invalid key generation.");
    return;
  }

  if (!state.matchedGroups[spId].matchedCustomers.some(
    (c) => c.RowKey === mergedPersonDocument.RowKey
  )) {
    state.matchedGroups[spId].matchedCustomers.push({
      ...mergedPersonDocument,
      similarity: similarity,
    });

    console.log(`✅ Added match: ${spId} with RowKey: ${mergedPersonDocument.RowKey}`);
    actions.saveMatchedGroupsToBackend({ state }, [mergedPersonDocument]);
  } else {
    console.warn(`⚠️ Duplicate match attempt (RowKey: ${mergedPersonDocument.RowKey})`);
  }
},

  //Unmatch Logic
  REMOVE_MATCH(state, { sharepointId, azureRowKey }) {
    if (state.matchedGroups[sharepointId]) {
      const removedItems = state.matchedGroups[
        sharepointId
      ].matchedCustomers.filter((c) => c.RowKey === azureRowKey);
      if (removedItems.length > 0) {
        const removedRevenue = removedItems.reduce(
          (sum, item) => sum + (item.calculatedRevenue || 0),
          0
        );
        console.log(
          `Removed match locally: Revenue lost: $${removedRevenue.toFixed(2)}`
        );
      }

      state.matchedGroups[sharepointId].matchedCustomers = state.matchedGroups[
        sharepointId
      ].matchedCustomers.filter((c) => c.RowKey !== azureRowKey); // แก้ไขตรงนี้ให้ Filter ด้วย c.RowKey

      if (state.matchedGroups[sharepointId].matchedCustomers.length === 0) {
        delete state.matchedGroups[sharepointId];
      }

      // ➡️ ลบข้อมูลออกจาก Backend โดยใช้ azureRowKey
      actions.deleteMatchedRecordFromBackend(null, azureRowKey);
    }
    // ➡️ ไม่ต้องเก็บใน localStorage.matchedGroups อีกต่อไป
    // localStorage.setItem("matchedGroups", JSON.stringify(state.matchedGroups));
    console.log("MatchedGroups after REMOVE_MATCH:", state.matchedGroups);
  },

  CLEAR_ALL_MATCHES(state) {
    state.matchedGroups = {};
    // ➡️ ไม่ต้องลบ localStorage.matchedGroups อีกต่อไป
    // localStorage.removeItem("matchedGroups");
    console.log(
      "All matches cleared from local state. (Backend not affected by this action)"
    );
    // ➡️ หากต้องการให้ Clear ALL Match จาก Backend ด้วย ต้องเรียก API ทุก item
    // dispatch('clearAllMatchesFromBackend');
  },

  RESET_STATE(state) {
    state.sharePointData = [];
    state.azureTableData = [];
    state.selectedSharePointItem = null;
    state.similarItems = [];
    state.previouslyMatchedData = [];
    state.matchedGroups = {};
    state.showAllSimilar = false;
    state.loading = false;
    state.error = null;
    state.sharePointSortKey = "s9DWINEntryDate";
    state.sharePointSortDirection = "desc";
    state.azureSortKey = "similarity";
    state.azureSortDirection = "desc";
    // ➡️ ไม่ต้องลบ localStorage.matchedGroups อีกต่อไป
    // localStorage.removeItem("matchedGroups");
  },
};

const actions = {
  async initializeDataAndMatches({ dispatch, commit, state }) {
    commit("SET_LOADING", true);
    commit("SET_ERROR", null);
    try {
      await Promise.all([
        dispatch("loadSharePointData"),
        dispatch("loadAzureTableData"),
      ]);

      const mergedData = await azureService.getPreviouslyMergedData();
      commit("SET_PREVIOUSLY_MATCHED_DATA", mergedData);

      commit("RECONSTRUCT_MATCHED_GROUPS");
    } catch (error) {
      console.error("Data initialization failed:", error);
      commit("SET_ERROR", `Failed to initialize data: ${error.message}`);
    } finally {
      commit("SET_LOADING", false);
    }
  },

  async loadSharePointData({ commit }) {
  commit("SET_LOADING", true);
  commit("SET_ERROR", null);
  try {
    const rawData = await sharepointService.getSharePointData();

    if (!Array.isArray(rawData)) {
      throw new Error("Received data is not an array from SharePoint service.");
    }

    const filteredData = rawData.filter((item) => {
      return item.pipelineStage == "Design-Win";
    });

    console.log(
      `SharePoint: Fetched ${rawData.length} records, filtered down to ${filteredData.length} records`
    );

    commit("SET_SHAREPOINT_DATA", filteredData);
  } catch (error) {
    console.error("Failed to load SharePoint Data:", error);
    commit("SET_ERROR", `SharePoint Load Failed: ${error.message}`);
    commit("SET_SHAREPOINT_DATA", []);
  } finally {
    commit("SET_LOADING", false);
  }
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

  async matchItems(
    { commit, state },
    { sharePointItem, azureItem, similarity }
  ) {
    commit("ADD_MATCH", { sharePointItem, azureItem, similarity });
  },

  async unmatchItems({ commit }, { sharepointId, azureRowKey }) {
    commit("REMOVE_MATCH", { sharepointId, azureRowKey });
    
  },

  async saveMatchedGroupsToBackend({ state }, recordsToSave) {
    try {
      const payload = { records: recordsToSave };
      console.log("🗄️ Sending matched records to backend for saving:", payload);
      const response = await azureService.updateMergedData(payload); // ใช้ azureService.updateMergedData
      console.log("✅ Matched records saved to backend:", response);
    } catch (error) {
      console.error("❌ Failed to save matched records to backend:", error);
      // ควรจัดการ error เช่น แสดงข้อความให้ผู้ใช้ทราบ
    }
  },

  // ➡️ Action สำหรับลบข้อมูล Matched Record จาก Backend
  async deleteMatchedRecordFromBackend(context, azureRowKey) {
    try {
      console.log(
        `🗑️ Requesting delete for matched record with RowKey: ${azureRowKey}`
      );
      const response = await azureService.deleteMergedDocument(azureRowKey);
      console.log("✅ Matched record deleted from backend:", response);
    } catch (error) {
      console.error(
        `❌ Failed to delete matched record ${azureRowKey} from backend:`,
        error
      );
      // ควรจัดการ error เช่น แสดงข้อความให้ผู้ใช้ทราบ
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

  clearAllMatches({ commit }) {
    commit("CLEAR_ALL_MATCHES");
    // หากต้องการให้ clear ทั้งหมดใน backend ต้องเรียก API ตาม id ของแต่ละ Record
    // ใน context ของ matchedGroups คุณจะต้องวนลูป matchedGroups และเรียก deleteMatchedRecordFromBackend
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
  setSharePointSort({ commit, state }, { key, direction }) {
    // direction อาจจะถูกส่งมาด้วย
    let actualDirection = direction;
    if (!actualDirection) {
      // ถ้าไม่ได้ส่งมาให้ toggle เอง
      actualDirection =
        state.sharePointSortConfig.direction === "asc" ? "desc" : "asc";
    }

    if (state.sharePointSortKey === key && direction === undefined) {
      // ถ้า key เดิมและไม่ได้ระบุ direction
      actualDirection =
        state.sharePointSortDirection === "asc" ? "desc" : "asc";
    } else if (key === "customerName" || key === "opportunityId") {
      // default asc สำหรับ text
      actualDirection = "asc";
    } else if (key === "calculatedRevenue" || key === "s9DWINEntryDate") {
      // default desc สำหรับ revenue/date
      actualDirection = "desc";
    } else {
      // ถ้าเป็น key ใหม่เริ่มต้นด้วย desc
      actualDirection = "desc";
    }

    commit("SET_SHAREPOINT_SORT", { key, direction: actualDirection });
  },
  // --- เพิ่ม Action สำหรับ Sorting Azure Data ---
  setAzureSort({ commit, state }, { key, direction }) {
    // direction อาจจะถูกส่งมาด้วย
    let actualDirection = direction;
    if (!actualDirection) {
      // ถ้าไม่ได้ส่งมาให้ toggle เอง
      actualDirection =
        state.azureSortConfig.direction === "asc" ? "desc" : "asc";
    }

    if (state.azureSortKey === key && direction === undefined) {
      // ถ้า key เดิมและไม่ได้ระบุ direction
      actualDirection = state.azureSortDirection === "asc" ? "desc" : "asc";
    } else if (key === "customerName") {
      // default asc สำหรับ text
      actualDirection = "asc";
    } else if (
      key === "similarity" ||
      key === "documentDate" ||
      key === "potentialRevenue"
    ) {
      // default desc สำหรับ score/date/revenue
      actualDirection = "desc";
    } else {
      // ถ้าเป็น key ใหม่เริ่มต้นด้วย desc
      actualDirection = "desc";
    }

    commit("SET_AZURE_SORT", { key, direction: actualDirection });
  },
};

export default {
  namespaced: true,
  state,
  getters,
  mutations,
  actions,
};
