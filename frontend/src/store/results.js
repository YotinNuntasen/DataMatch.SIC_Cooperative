import { exportToCSV, exportToExcel } from "../utils/csvExport";

const state = {
  exportData: [],
  exportFormat: "excel",
  fileName: "matched-data-export",
  loading: false,
  error: null,
  exportHistory: [],
  availableColumns: [],
  selectedColumns: [],
};

const getters = {
  // ---- Basic Getters ----
  exportData: (state) => state.exportData || [],
  exportFormat: (state) => state.exportFormat,
  fileName: (state) => state.fileName,
  loading: (state) => state.loading,
  error: (state) => state.error,
  exportHistory: (state) => state.exportHistory || [],
  availableColumns: (state) => state.availableColumns || [],
  selectedColumns: (state) => state.selectedColumns || [],

  // ---- Safe Getters with Fallback ----
  safeExportData: (state) =>
    Array.isArray(state.exportData) ? state.exportData : [],
  safeExportHistory: (state) =>
    Array.isArray(state.exportHistory) ? state.exportHistory : [],

  // ---- Statistics ----
  totalRecords: (state, getters) => getters.safeExportData.length,
  exportCount: (state, getters) => getters.safeExportHistory.length,

  exportSummary: (state, getters) => {
    const data = getters.safeExportData;
    return {
      total: data.length,
      matched: data.length,
    };
  },

  // 🔥🔥🔥 Getter สำคัญที่ดึงข้อมูลรายได้มาจาก dataMatching store 🔥🔥🔥
  totalRevenue: (state, getters, rootState, rootGetters) => {
    // ใช้ rootGetters เพื่อเข้าถึง getter จาก module อื่น
    return rootGetters["dataMatching/totalRevenue"] || 0;
  },
};

const mutations = {
  SET_EXPORT_DATA(state, data) {
    state.exportData = Array.isArray(data) ? data : [];
  },
  SET_EXPORT_FORMAT(state, format) {
    state.exportFormat = format || "excel";
  },
  SET_FILE_NAME(state, fileName) {
    state.fileName = fileName || "matched-data-export";
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
  ADD_EXPORT_HISTORY(state, exportRecord) {
    if (!Array.isArray(state.exportHistory)) {
      state.exportHistory = [];
    }
    state.exportHistory.unshift({
      id: Date.now(),
      timestamp: new Date().toISOString(),
      ...exportRecord,
    });
    // จำกัดให้แสดงแค่ 10 รายการล่าสุด
    if (state.exportHistory.length > 10) {
      state.exportHistory = state.exportHistory.slice(0, 10);
    }
  },
  SET_AVAILABLE_COLUMNS(state, columns) {
    state.availableColumns = Array.isArray(columns) ? columns : [];
  },
  SET_SELECTED_COLUMNS(state, columns) {
    state.selectedColumns = Array.isArray(columns) ? columns : [];
  },
  CLEAR_EXPORT_HISTORY(state) {
    state.exportHistory = [];
  },
  RESET_STATE(state) {
    state.exportData = [];
    state.exportFormat = "excel";
    state.fileName = "matched-data-export";
    state.loading = false;
    state.error = null;
    state.exportHistory = [];
    state.availableColumns = [];
    state.selectedColumns = [];
  },
};

const actions = {
  // Action หลักสำหรับเริ่มกระบวนการ Export
  async exportFile({ state, dispatch }) {
    if (!state.exportData || state.exportData.length === 0) {
      dispatch("setError", "No data to export.");
      return;
    }

    try {
      // ส่ง selectedColumns ไปให้ action ย่อย
      const options = { selectedColumns: state.selectedColumns };

      if (state.exportFormat === "excel") {
        await dispatch("exportToExcel", options);
      } else if (state.exportFormat === "csv") {
        await dispatch("exportToCsv", options);
      } else {
        throw new Error(`Invalid export format: ${state.exportFormat}`);
      }
    } catch (error) {
      console.error("Export process failed:", error);
    
    }
  },

  prepareExportData({ commit, rootGetters }) {
    commit("SET_LOADING", true);
    try {
      const matchedPairs = rootGetters["dataMatching/exportData"];

      if (!matchedPairs || matchedPairs.length === 0) {
        console.warn("No matched data found from dataMatching store.");
        commit("SET_EXPORT_DATA", []);
        commit("SET_AVAILABLE_COLUMNS", []);
        commit("SET_SELECTED_COLUMNS", []);
        return;
      }

      commit("SET_EXPORT_DATA", matchedPairs);

      const selectedColumns = [
        'SP: opportunityName',
        'SP: opportunityId',
        'AZ: selltoCustName_SalesHeader',
        'AZ: custShortDimName',
        'AZ: itemReferenceNo',
        'AZ: documentDate',
        'SP: s9DWINEntryDate',
        'AZ: salespersonDimName',
        'AZ: prodChipNameDimName',
        'AZ: quantity',
        'AZ: sellToCustomerNo',
        'AZ: totalSales',
        'AZ: description',
      ];


      commit("SET_AVAILABLE_COLUMNS", selectedColumns);
    commit("SET_SELECTED_COLUMNS", selectedColumns);

    console.log(`Prepared ${matchedPairs.length} records for export with ${selectedColumns.length} columns.`);
  } catch (e) {
    console.error("Failed to prepare export data:", e);
    commit("SET_ERROR", "Could not prepare data for results page.");
  } finally {
    commit("SET_LOADING", false);
  }
},

  // Actions สำหรับตั้งค่า
  updateSelectedColumns({ commit }, columns) {
    commit("SET_SELECTED_COLUMNS", columns);
  },
  setExportFormat({ commit }, format) {
    commit("SET_EXPORT_FORMAT", format);
  },
  setFileName({ commit }, fileName) {
    commit("SET_FILE_NAME", fileName);
  },

  // Action ย่อยสำหรับ Export เป็น Excel
  async exportToExcel({ state, commit, dispatch }, options) {
    // รับ options เข้ามา
    commit("SET_LOADING", true);
    commit("CLEAR_ERROR");
    try {
      // ✅ ใช้ utility function ที่ import มา
      await exportToExcel(
        state.exportData,
        `${state.fileName}.xlsx`,
        options.selectedColumns
      );

      commit("ADD_EXPORT_HISTORY", {
        fileName: `${state.fileName}.xlsx`,
        format: "excel",
        recordCount: state.exportData.length,
        success: true,
      });

      console.log("✅ Excel export completed");
    } catch (error) {
      console.error("❌ Excel export failed:", error);
      commit("SET_ERROR", error.message);
      commit("ADD_EXPORT_HISTORY", {
        fileName: `${state.fileName}.xlsx`,
        format: "excel",
        recordCount: 0,
        success: false,
      });
    } finally {
      commit("SET_LOADING", false);
    }
  },

  // Action ย่อยสำหรับ Export เป็น CSV
  async exportToCsv({ state, commit, dispatch }, options) {
    // รับ options เข้ามา
    commit("SET_LOADING", true);
    commit("CLEAR_ERROR");
    try {
      // ✅ ใช้ utility function ที่ import มา
      await exportToCSV(
        state.exportData,
        `${state.fileName}.csv`,
        options.selectedColumns
      );

      commit("ADD_EXPORT_HISTORY", {
        fileName: `${state.fileName}.csv`,
        format: "csv",
        recordCount: state.exportData.length,
        success: true,
      });

      // (Optional) หากมี notification system
      // dispatch("notifications/showSuccess", "CSV file exported successfully!", { root: true });

      console.log("✅ CSV export completed");
    } catch (error) {
      console.error("❌ CSV export failed:", error);
      commit("SET_ERROR", error.message);
      commit("ADD_EXPORT_HISTORY", {
        fileName: `${state.fileName}.csv`,
        format: "csv",
        recordCount: 0,
        success: false,
      });
    } finally {
      commit("SET_LOADING", false);
    }
  },

  // Actions สำหรับจัดการ state
  clearExportHistory({ commit }) {
    commit("CLEAR_EXPORT_HISTORY");
  },
  resetResults({ commit }) {
    commit("RESET_STATE");
  },
};

export default {
  namespaced: true,
  state,
  getters,
  mutations,
  actions,
};
