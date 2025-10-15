<template>
  <div class="results-preview">
    <!-- Header -->
    <div class="page-header">
      <div class="header-content">
        <div>
          <h1>Results Preview</h1>
          <p>Review matched data before export or update</p>
        </div>
        <div class="header-actions">
          <button @click="goBack" class="back-btn">
            ← Back to Matching
          </button>
        </div>
      </div>
    </div>

    <!-- Summary Cards -->
    <div class="summary-section">
      <div class="summary-cards">
        <div class="summary-card total">
          <div class="card-icon">📊</div>
          <div class="card-content">
            <div class="card-number">{{ exportSummary.total }}</div>
            <div class="card-label">Total Records</div>
          </div>
        </div>
        <div class="summary-card matched">
          <div class="card-icon">✅</div>
          <div class="card-content">
            <div class="card-number">{{ exportSummary.matched }}</div>
            <div class="card-label">Matched Pairs</div>
          </div>
        </div>
        <div class="summary-card accuracy">
          <div class="card-icon">💰</div>
          <div class="card-content">
            <div class="card-number">{{ formatCurrency(totalRevenue) }}</div>
            <div class="card-label">Total Revenue</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Export Options -->
    <div class="export-section">
      <div class="export-header">
        <h2>Export Options</h2>
      </div>
      <div class="export-options">
        <div class="option-group">
          <label for="exportFormat">Export Format:</label>
          <select id="exportFormat" v-model="localExportFormat" @change="updateExportFormat" class="input-field">
            <option value="excel">Excel (.xlsx)</option>
            <option value="csv">CSV (.csv)</option>
          </select>
        </div>
        <div class="option-group">
          <label for="fileName">File Name:</label>
          <input id="fileName" v-model="localFileName" @blur="updateFileName" type="text" class="input-field" />
        </div>
      </div>
      <div class="export-actions">
        <button @click="handleExport" :disabled="safeExportData.length === 0 || loading" class="action-btn export-btn">
          <span v-if="loading">Exporting...</span>
          <span v-else>📤 Export {{ localExportFormat.toUpperCase() }}</span>
        </button>
        <button @click="previewData" :disabled="safeExportData.length === 0" class="action-btn preview-btn">
          {{ showPreview ? 'Hide Preview' : '👁️ Review Data' }}
        </button>
        <button @click="handleUpdate" :disabled="safeExportData.length === 0 || isUpdating"
          class="action-btn update-btn">
          <span v-if="isUpdating"><span class="spinner"></span>Updating...</span>
          <span v-else>🗄️ Save Match Data </span>
        </button>
      </div>
    </div>

    <!-- Preview Section -->
    <div v-if="showPreview && safeExportData.length > 0" class="preview-section">
      <div class="table-container">
        <table class="preview-table">
          <thead>
            <tr>
              <th>OpportunityName</th>
              <th>OpportunityId</th>
              <th>Customer Name</th>
              <th>ShortName</th>
              <th>PCode</th>
              <th>documentDate</th>
              <th>Dwin Date</th>
              <th>SalesName</th>
              <th>ChipName</th>
              <th>Quantity</th>
              <th>Customer No</th>
              <th>Total Sales</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(item, index) in previewItems" :key="index">
              <td>{{ item.sharepoint?.opportunityName || 'N/A' }}</td>
              <td>{{ item.sharepoint?.opportunityId || 'N/A' }}</td>
              <td>{{ item.azure?.selltoCustName_SalesHeader || 'N/A' }}</td>
              <td>{{ item.azure?.custShortDimName || 'N/A' }}</td>
              <td>{{ item.azure?.itemReferenceNo || 'N/A' }}</td>
              <td>{{ formatDate(item.azure?.documentDate) }}</td>
              <td>{{ formatDate(item.sharepoint?.s9DWINEntryDate) }}</td>
              <td>{{ item.azure?.salespersonDimName || 'N/A' }}</td>
              <td>{{ item.azure?.prodChipNameDimName || 'N/A' }}</td>
              <td>{{ item.azure?.quantity || 'N/A' }}</td>
              <td>{{ item.azure?.sellToCustomerNo || 'N/A' }}</td>
              <td>{{ formatCurrency(item.azure?.totalSales) }}</td>
              <td>{{ item.azure?.description || 'N/A' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- History Section (ถ้ามี) -->
    <div v-if="safeExportHistory.length > 0" class="history-section">
      <div class="history-header">
        <h3>Export History</h3>
        <button @click="clearHistory" class="clear-history-btn">
          Clear History
        </button>
      </div>
      <div class="history-list">
        <div v-for="record in safeExportHistory" :key="record.id" class="history-item">
          <div class="history-info">
            <span class="history-filename">{{ record.fileName }}</span>
            <span class="history-details">{{ record.recordCount }} records • {{ formatDate(record.timestamp, true)
            }}</span>
          </div>
          <div class="history-status" :class="{ 'status-success': record.success, 'status-error': !record.success }">
            {{ record.success ? '✅ Success' : '❌ Failed' }}
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script>
import { mapGetters, mapActions } from 'vuex';
import azureService from '../services/azureService';
import { formatDate, formatCurrency } from '@/utils/formatters';

export default {
  name: 'ResultsPreview',
  data() {
    return {
      showPreview: false,
      localExportFormat: 'excel',
      localFileName: 'matched-data-export',
      isUpdating: false,
    };
  },
  computed: {
    ...mapGetters('results', [
      'exportData',
      'exportFormat',
      'fileName',
      'loading',
      'error',
      'exportHistory',
      'exportSummary',
      'totalRevenue'
    ]),
    safeExportData() { return Array.isArray(this.exportData) ? this.exportData : []; },
    safeExportHistory() { return Array.isArray(this.exportHistory) ? this.exportHistory : []; },
    previewItems() { return this.safeExportData.slice(0, 10); },
  },
  methods: {
    ...mapActions('results', [
      'prepareExportData',
      'setExportFormat',
      'setFileName',
      'exportFile',
      'clearExportHistory'
    ]),

    // ✅ Expose formatters from utils
    formatCurrency,

    // ✅ Wrapper for formatDate with includeTime option
    formatDate(dateStr, includeTime = false) {
      if (!dateStr) return 'N/A';

      const options = includeTime
        ? { dateStyle: 'medium', timeStyle: 'short' }
        : { dateStyle: 'medium' };

      try {
        return new Intl.DateTimeFormat('en-GB', options).format(new Date(dateStr));
      } catch {
        return 'Invalid Date';
      }
    },

    goBack() { this.$router.push({ name: 'DataMatching' }); },

    previewData() { this.showPreview = !this.showPreview; },

    clearHistory() {
      if (confirm('Are you sure you want to clear the export history?')) {
        this.clearExportHistory();
      }
    },

    updateExportFormat() { this.setExportFormat(this.localExportFormat); },

    updateFileName() { this.setFileName(this.localFileName.trim()); },

    async handleExport() { await this.exportFile(); },

    async handleUpdate() {
      if (this.safeExportData.length === 0) {
        this.$toast.warning("No data available to update.");
        return;
      }

      // ✨ เปลี่ยนเป็น SweetAlert2 ที่สวยงาม
      this.$swal.fire({
        title: 'Are you sure?',
        text: `This will replace all existing data with these ${this.safeExportData.length} records. This action cannot be undone.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#16A34A', // สีเขียว
        cancelButtonColor: '#DC2626', // สีแดง
        confirmButtonText: 'Yes, update it!',
        customClass: {
          popup: 'swal2-custom-popup',
          title: 'swal2-custom-title',
          confirmButton: 'swal2-custom-confirm-button',
          cancelButton: 'swal2-custom-cancel-button'
        }
      }).then(async (result) => {
        // เช็คว่าผู้ใช้กดปุ่ม "Yes, update it!" หรือไม่
        if (result.isConfirmed) {
          this.isUpdating = true;
          try {
            const payload = this.prepareUpdatePayload();
            const response = await azureService.replaceMergedData(payload);

            // ✨ แสดง Toast พร้อมจำนวนข้อมูลที่บันทึกสำเร็จ
            const successCount = response.data?.insertedCount || payload.records.length;
            this.$toast.success(`${successCount} records have been saved successfully!`);

            console.log("Replace result:", response.data);

          } catch (error) {
            const errorMessage = error.response?.data?.message || error.message || 'An unknown error occurred during update.';
            this.$toast.error(`Update failed: ${errorMessage}`);
            console.error("Update error:", error);
          } finally {
            this.isUpdating = false;
          }
        }
      });
    },

    prepareUpdatePayload() {
      return {
        records: this.safeExportData
          .filter(item => item && item.sharepoint && item.azure)
          .map(item => ({
            PartitionKey: item.azure.PartitionKey,
            RowKey: item.azure.RowKey,
            opportunityId: item.sharepoint.opportunityId,
            opportunityName: item.sharepoint.opportunityName,
            CustShortDimName: item.azure.custShortDimName,
            PrefixdocumentNo: item.azure.prefixdocumentNo,
            SelltoCustName_SalesHeader: item.azure.selltoCustName_SalesHeader,
            SystemRowVersion: item.azure.systemRowVersion,
            DocumentDate: item.azure.documentDate,
            documentNo: item.azure.documentNo,
            itemReferenceNo: item.azure.itemReferenceNo,
            lineNo: item.azure.lineNo,
            no: item.azure.no,
            quantity: item.azure.quantity,
            sellToCustomerNo: item.azure.sellToCustomerNo,
            shipmentNo: item.azure.shipmentNo,
            sodocumentNo: item.azure.sodocumentNo,
            description: item.azure.description,
            unitPrice: item.azure.unitPrice,
            LineDiscount: item.azure.lineDiscount,
            lineAmount: item.azure.lineAmount,
            CurrencyRate: item.azure.currencyRate,
            SalesPerUnit: item.azure.salesPerUnit,
            TotalSales: item.azure.totalSales,
            CustAppDimName: item.azure.custAppDimName,
            ProdChipNameDimName: item.azure.prodChipNameDimName,
            RegionDimName3: item.azure.regionDimName3,
            SalespersonDimName: item.azure.salespersonDimName,
          }))
      };
    },
  },

  created() {
    this.prepareExportData();
    this.localExportFormat = this.exportFormat || 'excel';
    this.localFileName = this.fileName || 'matched-data-export';
  }
};
</script>

<style scoped>
.results-preview {
  margin: 0 auto;
  padding: 40px;
  min-height: 100vh;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  background: #ccdfff;
}

/* Page Header */
.page-header {
  margin-bottom: 30px;

}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  flex-wrap: wrap;
  gap: 10px;
  border-bottom: 2px solid #ffffff;
  padding-bottom: 20px;
}

.header-content h1 {
  font-size: 3rem;
  color: #225383;
  margin: 0;
  font-weight: 600;
  font-style: oblique;
}

.header-content p {
  font-size: 1.5rem;
  color: #6B7C93;
  font-weight: 500;
  margin: 5px 0 0 0;
}

.back-btn {
  background: transparent;
  color: #3292ff;
  border: 1px solid #0077ff;
  background-color: whitesmoke;
  padding: 12px 20px;
  border-radius: 10px;
  font-weight: 600;
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.2s ease-in-out;
}

.back-btn:hover {
  background: #4A90E2;
  color: white;
}

/* Sections */
.summary-section,
.export-section,
.preview-section,
.history-section {
  background: #FFFFFF;
  border-radius: 12px;
  padding: 25px;
  margin-bottom: 30px;
  box-shadow: 0 4px 12px rgba(0, 30, 80, 0.06);
}

/* Summary Cards */
.summary-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 25px;
}

.summary-card {
  background: #F9FAFB;
  border: 2px solid #E5E7EB;
  padding: 20px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  gap: 15px;
  transition: transform 0.2s, box-shadow 0.2s;
}

.summary-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 6px 16px rgba(0, 30, 80, 0.1);
}

.card-icon {
  font-size: 2rem;
}

.card-content {
  flex-grow: 1;
}

.card-number {
  font-size: 2.2rem;
  font-weight: 700;
  color: #2F69C6;
}

.card-label {
  font-size: 1rem;
  color: #6B7C93;
  font-weight: 500;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

/* Export Section */
.export-header h2 {
  font-size: 2rem;
  color: #1A3A5A;
  margin: 0 0 20px 0;
  padding-bottom: 10px;
  border-bottom: 2px solid #E5E7EB;
}

.export-options {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 20px;
  margin-bottom: 25px;
}

.option-group label {
  display: block;
  font-weight: 600;
  color: #374151;
  margin-bottom: 10px;
}

.input-field {
  width: 100%;
  font-weight: 300;
  padding: 18px;
  border: 1px solid #D1D5DB;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.2s, box-shadow 0.2s;
  box-sizing: border-box;
  /* Important for width calculation */
}

.input-field:focus {
  outline: none;
  border-color: #4A90E2;
  box-shadow: 0 0 0 3px rgba(74, 144, 226, 0.2);
}

.export-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 15px;
}

.action-btn {
  padding: 12px 24px;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease-in-out;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.action-btn:disabled {
  background-color: #D1D5DB !important;
  color: #9CA3AF !important;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.export-btn {
  background-color: #2563EB;
  color: white;
}

.export-btn:hover:not(:disabled) {
  background-color: #1D4ED8;
}

.preview-btn {
  background-color: #F3F4F6;
  color: #1F2937;
  border: 1px solid #D1D5DB;
}

.preview-btn:hover:not(:disabled) {
  background-color: #E5E7EB;
}

.update-btn {
  background-color: #16A34A;
  color: white;
}

.update-btn:hover:not(:disabled) {
  background-color: #15803D;
}

.swal2-custom-popup {
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  border-radius: 12px !important;
}
.swal2-custom-title {
  color: #1A3A5A !important;
}
.swal2-custom-confirm-button, .swal2-custom-cancel-button {
  border-radius: 8px !important;
  font-weight: 600 !important;
  padding: 10px 24px !important;
  box-shadow: none !important; 
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: white;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.table-container {
  overflow-x: auto;
  width: 100%;
}

.preview-table {
  width: 100%;
  min-width: 1800px;
  border-collapse: collapse;
  font-size: 0.9rem;
}

.preview-table th,
.preview-table td {
  padding: 12px 15px;
  text-align: left;
  border: 1px solid #E5E7EB;
  white-space: nowrap;
  vertical-align: middle;
}

.preview-table thead th {
  background-color: #F9FAFB;
  font-weight: 600;
  color: #374151;
  position: sticky;
  top: 0;
}

.preview-table tbody tr:nth-child(even) {
  background-color: #F9FAFB;
}

.preview-table tbody tr:hover {
  background-color: #EFF6FF;
}

/* History Section */
.history-section h3 {
  font-size: 1.5rem;
  color: #1A3A5A;
  margin: 0 0 20px 0;
}

.history-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 15px;
  border-bottom: 1px solid #E5E7EB;
}

.clear-history-btn {
  background: #FEF2F2;
  color: #DC2626;
  border: 1px solid #FCA5A5;
  padding: 6px 12px;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.clear-history-btn:hover {
  background: #DC2626;
  color: white;
}

.history-list {
  margin-top: 20px;
}

.history-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 10px;
  border-bottom: 1px solid #E5E7EB;
}

.history-item:last-child {
  border-bottom: none;
}

.history-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.history-filename {
  font-weight: 500;
  color: #1F2937;
}

.history-details {
  font-size: 0.85rem;
  color: #6B7C93;
}

.history-status {
  font-weight: 600;
}

.status-success {
  color: #16A34A;
}

.status-error {
  color: #DC2626;
}
</style>