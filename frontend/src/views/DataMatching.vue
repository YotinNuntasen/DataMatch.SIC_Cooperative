<template>
  <div class="data-matching-page">
    <!-- Header -->
    <div class="page-header">
      <div class="header-content">
        <div class="header-left">
          <h1>Data Matching</h1>
          <p>Match SharePoint opportunities with Azure customer records</p>
        </div>

        <div class="header-actions">
          <div class="user-info">
            <span class="user-name">{{ userName }}</span>
            <button @click="logout" class="logout-btn">Logout</button>
          </div>
        </div>
      </div>

      <div class="progress-bar">
        <div class="progress-step active">
          <span class="step-number">1</span>
          <span class="step-label">Login</span>
        </div>
        <div class="progress-step active">
          <span class="step-number">2</span>
          <span class="step-label">Match Data</span>
        </div>
        <div class="progress-step">
          <span class="step-number">3</span>
          <span class="step-label">Results</span>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <div class="matching-content">
      <div class="outlook-layout">

        <!-- Left Panel: SharePoint Opportunities -->
        <div class="mail-list-panel">
          <div class="panel-header">
            <h2>SharePoint Opportunities</h2>
            <!-- ใช้ displaySharePointData -->
            <span class="item-count">{{ displaySharePointData.length }} items</span>
          </div>


          <div class="sort-controls">
            <label for="sp-sort-key" class="sort-label">Sort by:</label>
            <select id="sp-sort-key" v-model="currentSharePointSortKey" @change="changeSharePointSort"
              class="sort-select">
              <option value="s9DWINEntryDate">DWIN Date</option>
              <option value="calculatedRevenue">Revenue</option>
              <option value="customerName">Customer Name</option>
              <option value="opportunityId">Opportunity ID</option>
            </select>
            <button @click="toggleSharePointSortDirection" class="sort-direction-btn">
              <span v-if="sharePointSortConfig.direction === 'asc'">↑ Asc</span>
              <span v-else>↓ Desc</span>
            </button>
          </div>
          <!-- --- ⬆️ สิ้นสุดการเปลี่ยนแปลงสำหรับ Sorting SharePoint Opportunities ⬆️ --- -->


          <div class="mail-list-container"> 
            <div v-for="item in displaySharePointData" :key="item.id" class="mail-item" :class="{
              'selected': selectedSharePointItem?.id === item.id,
              'has-matches': hasMatches(item.id)
            }" @click="selectItem(item)">
              <div class="mail-header">
                <div class="mail-meta">
                  <span class="opportunity-id">{{ item.opportunityId || 'N/A' }}</span>
                  <span class="total-revenue">{{ formatCurrency(item.calculatedRevenue) }}</span>
                </div>
                <div class="match-indicator" v-if="hasMatches(item.id)">
                  <span class="match-count">{{ getMatchCount(item.id) }}</span>
                </div>
              </div>

              <div class="mail-subject">
                <h4>{{ item.customerName || 'Unknown Customer' }}</h4>
              </div>

              <div class="mail-preview">
                <div class="preview-line">
                  <span class="label">Opportunity Name:</span>
                  <span class="value">{{ item.opportunityName || 'N/A' }}</span>
                </div>
                <div class="preview-line">
                  <span class="label">Product:</span>
                  <span class="value">{{ item.productGroup || 'N/A' }}</span>
                </div>
                <div class="preview-line">
                  <span class="label">Product:</span>
                  <span class="value">{{ item.productCode || 'N/A' }}</span>
                </div>
                <div class="preview-line">
                  <span class="label">DWIN Date:</span>
                  <span class="value">{{ formatDate(item.s9DWINEntryDate) || 'N/A' }}</span>
                </div>
              </div>
            </div>

            <div v-if="displaySharePointData.length === 0" class="empty-mail-list">
              <p>No SharePoint opportunities available</p>
              <!-- initializeDataAndMatches -->
              <button @click="initializeDataAndMatches" class="reload-btn">Reload Data</button>
            </div>
          </div>
        </div>

        <!-- Right Panel: Selected Item + Azure Table (top) + Matched (bottom) -->
        <div class="detail-panel">
          <div v-if="!selectedSharePointItem" class="no-selection-state">
            <div class="selection-message-wrapper">
              <div class="prompt-icon">📧</div>
              <div class="selection-text-content">
                <h3>Select an Opportunity</h3>
                <p>Choose a SharePoint opportunity from the list to view matching Azure customer records</p>
              </div>
            </div>
          </div>
          <div v-else class="detail-content">
            <!-- Selected Item Header -->
            <div class="selected-item-header">
              <h2>{{ selectedSharePointItem.customerName || 'Unknown Customer' }}</h2>
              <div class="item-meta">
                <span class="meta-item">ID: {{ selectedSharePointItem.opportunityId }}</span>
                <span class="meta-item">Product: {{ selectedSharePointItem.productGroup || 'N/A' }}</span>
                <span class="meta-item">DWIN: {{ formatDate(selectedSharePointItem.s9DWINEntryDate) }}</span>
              </div>
            </div>

            <!-- TOP: Similar header (yellow) + AzureFullTable -->
            <div class="azure-top">
              <div class="similar-header-top">
                <div class="similar-left">
                  <h3>Similar Customer Records</h3>
                  <span class="similarity-info">Top {{ Math.min(8, safeSimilarItems.length) }} matches</span>
                </div>
                <div class="similar-actions">
                  <button v-if="!showAllSimilar && safeSimilarItems.length > 8" @click="showAllItems"
                    class="show-all-btn">
                    Show All ({{ safeSimilarItems.length }})
                  </button>
                  <button v-if="showAllSimilar" @click="hideAllItems" class="hide-btn">Show Top 8</button>
                </div>
              </div>

              <div class="azure-fulltable-wrapper">
                <AzureFullTable :similar-records="safeDisplayedSimilarItems"
                  :selected-share-point-item="selectedSharePointItem" :azure-sort-config="azureSortConfig"
                  @match="handleMatch" @sort-azure="setAzureSort" />
              </div>
            </div>

            <!-- BOTTOM: Matched items + summary controls -->
            <div class="sections-container">
              <div class="matched-items-section" v-if="getMatchedItems(selectedSharePointItem.id).length > 0">
                <div class="section-header matched-header">
                  <h3>Matched Records</h3>
                  <span class="count-badge">{{ getMatchedItems(selectedSharePointItem.id).length }}</span>
                </div>

                <div class="matched-items-container">
                  <div v-for="azureItem in getMatchedItems(selectedSharePointItem.id)" :key="azureItem.RowKey"
                    class="matched-azure-card">
                    <div class="card-header">
                      <div class="similarity-score matched">
                        {{ azureItem.documentNo }}
                      </div>
                    
                      <button @click="handleUnmatch(selectedSharePointItem.id, azureItem.RowKey)" class="unmatch-btn"
                        title="Unmatch">×</button>
                    </div>

                    <div class="card-content">
                      <h4>{{ azureItem.custShortDimName || 'Unknown Name' }}</h4>
                      <p class="email">{{ formatDate(azureItem.documentDate) || 'N/A' }}</p>
                      <div class="details">
                        <span class="detail">{{ azureItem.salespersonDimName || 'N/A' }}</span>
                        <span class="detail">{{ azureItem.prodChipNameDimName || 'N/A' }}</span>
                        <span class="detail">{{ formatCurrency(azureItem.calculatedRevenue) || 'N/A' }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- If no matched items, keep the similar summary area informative -->
              <div v-if="getMatchedItems(selectedSharePointItem.id).length === 0" class="empty-state">
                <p>No matched records for this opportunity yet</p>
              </div>
            </div>
          </div>
        </div>

      </div>
    </div>

    <!-- Footer -->
    <div class="page-footer">
      <div class="footer-content">
        <div class="match-summary">
          <span class="summary-text">
            {{ safeMatchedPairs.length }} pairs matched
          </span>
        </div>

        <button @click="goToResults" :disabled="safeMatchedPairs.length === 0" class="next-btn">
          View Results →
        </button>
      </div>
    </div>
  </div>
</template>

<script>
import { mapGetters, mapActions } from 'vuex'
import AzureFullTable from '../components/AzureFullTable.vue'

export default {
  name: 'DataMatching',
  components: { AzureFullTable },
  data() {
    return {
      showDebug: process.env.NODE_ENV === 'development',
      currentSharePointSortKey: 's9DWINEntryDate',
    }
  },
  computed: {
    ...mapGetters('auth', ['userName']),
    ...mapGetters('dataMatching', [
      'enhancedSharePointData',
      'azureTableData',
      'selectedSharePointItem',
      'similarItems',
      'topSimilarItems',
      'flatMatchedPairs',
      'showAllSimilar',
      'unmatchedSharePointData',
      'loading',
      'error',
      'displaySharePointData', 
      'sharePointSortConfig',
      'azureSortConfig',
    ]),

    safeSharePointData() { return this.displaySharePointData || []; },
    safeMatchedPairs() { return this.flatMatchedPairs || []; },
    safeSimilarItems() { return this.similarItems || []; },
    safeTopSimilarItems() { return this.topSimilarItems || []; },

    safeDisplayedSimilarItems() {
      const items = this.showAllSimilar ? this.safeSimilarItems : this.safeTopSimilarItems;
      if (!this.selectedSharePointItem) return items;

      const currentGroup = this.$store.state.dataMatching.matchedGroups[this.selectedSharePointItem.id];
      const matchedRowKeys = currentGroup ? currentGroup.matchedCustomers.map(m => m.RowKey) : [];

      const filtered = items.filter(item => {
        const rowKey = item.RowKey || item.rowKey;
        const isMatched = matchedRowKeys.includes(rowKey);
        return !isMatched;
      });

      return filtered;
    }
  },

  watch: {
    'sharePointSortConfig.key': {
      immediate: true,
      handler(newKey) {
        if (this.currentSharePointSortKey !== newKey) {
          this.currentSharePointSortKey = newKey;
        }
      }
    }
  },
  
  async created() {
    
    await this.initializeDataAndMatches();
  },

  methods: {
    ...mapActions('dataMatching', [
      'loadSharePointData', 
      'loadAzureTableData', 
      'initializeDataAndMatches', 
      'selectSharePointItem',
      'matchItems',
      'unmatchItems',
      'showAllSimilarItems',
      'hideAllSimilarItems',
      'clearAllMatches',
      'resetState',
      'setSharePointSort',
      'setAzureSort',
    ]),

   
    selectItem(item) {
      if (!item) return;
      this.selectSharePointItem(item);
    },

    handleMatch({ sharePointItem, azureItem, similarity }) {
      this.matchItems({ sharePointItem, azureItem, similarity });
    },

    handleUnmatch(sharepointId, azureRowKey) {
      this.unmatchItems({ sharepointId, azureRowKey });
    },

    showAllItems() { this.showAllSimilarItems(); },
    hideAllItems() { this.hideAllSimilarItems(); },

    goToResults() {
      if (this.safeMatchedPairs.length > 0) this.$router.push('/results');
    },

    hasMatches(sharePointId) {
      return this.safeMatchedPairs.some(pair => pair.sharepoint?.id === sharePointId);
    },

    getMatchCount(sharePointId) {
      return this.safeMatchedPairs.filter(pair => pair.sharepoint?.id === sharePointId).length;
    },

    getMatchedItems(sharePointId) {
      return this.safeMatchedPairs
        .filter(pair => pair.sharepoint?.id === sharePointId)
        .map(pair => ({
          ...pair.azure,
          calculatedRevenue: pair.calculatedRevenue,
          similarityScore: pair.azure.similarityScore,
          RowKey: pair.azure.RowKey
        }));
    },

    isItemMatched(sharePointId, azureRowKey) {
      if (!sharePointId || !azureRowKey) return false;
      return this.safeMatchedPairs.some(pair =>
        pair.sharepoint?.id === sharePointId && pair.azure?.RowKey === azureRowKey
      )
    },

    formatDate(dateString) {
      if (!dateString) return 'N/A';
      try {
        return new Date(dateString).toLocaleDateString('th-TH', {
          day: '2-digit', month: 'short', year: 'numeric'
        });
      } catch (e) { return 'N/A'; }
    },

    formatCurrency(value) {
      if (value === null || value === undefined || isNaN(value)) return 'N/A';
      if (value === 0) return '0';
      return new Intl.NumberFormat('th-TH', {
        style: 'currency',
        currency: 'THB',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
      }).format(value);
    },

    async logout() {
      try {
        await this.$store.dispatch('auth/logout');
        await this.$store.dispatch('dataMatching/resetState');
        await this.$router.push('/login').catch(() => { window.location.href = '/login'; });
      } catch (error) {
        console.error('Logout error:', error);
        window.location.href = '/login';
      }
    },

    changeSharePointSort() {
      this.setSharePointSort({ key: this.currentSharePointSortKey });
    },
    
    toggleSharePointSortDirection() {
      const newDirection = this.sharePointSortConfig.direction === 'asc' ? 'desc' : 'asc';
      this.setSharePointSort({ key: this.sharePointSortConfig.key, direction: newDirection });
    },
  }
}
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');

.data-matching-page {
  min-height: 100vh;
  width: 100vw;
  background: linear-gradient(-120deg, #2A4E84 0%, #B0E0FA 100%);
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  display: flex;
  flex-direction: column;
  margin-top: 0 !important;
  overflow-x: hidden;
}

/* Header */
.page-header {
  background: linear-gradient(135deg, #d0d9ff 0%, #9b48ee 100%);
  padding: 10px 20px;
  box-shadow: 0 2px 10px #0000001a;
  border-radius: 15px;
  flex-shrink: 0;
}

.header-content {
  width: 95%;
  max-width: 1800px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.header-left h1 {
  font-size: 2.2rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
  letter-spacing: -0.025em;
}

.header-left p {
  color: #475569;
  margin: 8px 0 0 0;
  font-size: 1.2rem;
  font-weight: 400;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 15px;
}

.user-name {
  font-weight: 600;
  color: #1e293b;
  font-size: 0.95rem;
}

.logout-btn {
  padding: 8px 16px;
  background: #ffffff;
  border: 1px solid #dc2626;
  border-radius: 8px;
  color: #dc2626;
  cursor: pointer;
  transition: all 0.3s ease;
  font-weight: 500;
  font-size: 0.9rem;
}

.logout-btn:hover {
  background: #fef2f2;
}

.progress-bar {
  max-width: 1000px;
  margin: 0 auto;
  display: flex;
  justify-content: center;
  gap: 150px;
}

.progress-step {
  display: flex;
  align-items: center;
  gap: 10px;
  color: #ffffff;
  font-weight: 600;
  font-size: 1rem;
}

.progress-step.active {
  color: #065f46;
}

.step-number {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background: #bebebe;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1rem;
}

.progress-step.active .step-number {
  background: #10b981;
  color: white;
}

/* Main layout */
.matching-content {
  flex: 1;
  width: 90%;
  margin: 0 auto;
  padding: 20px;
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
}

.outlook-layout {
  display: grid;
  grid-template-columns: 35% 1fr !important;
  gap: 0;
  flex-grow: 1;
  background: white;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
  height: calc(100vh - 250px);
}

/* Left Panel (list) */
.mail-list-panel {
  border-right: 2px solid #bec0c3;
  display: flex;
  flex-direction: column;
  background: #fafbfc;
  min-width: 250px;
  height: 100%;
  overflow: hidden;
}

.panel-header {
  background: #f3f2f1;
  padding: 16px 20px;
  border-bottom: 1px solid #e1e5e9;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;
}

.panel-header h2 {
  font-size: 1rem;
  font-weight: 600;
  color: #323130;
  margin: 0;
}

.item-count {
  font-size: 0.8rem;
  color: #666;
  background: #e1dfdd;
  padding: 3px 8px;
  border-radius: 12px;
  font-weight: 500;
}

/* --- SharePoint Sort Controls --- */
.sort-controls {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 20px;
  background: #f3f2f1;
  border-bottom: 1px solid #e1e5e9;
  flex-shrink: 0;
}

.sort-label {
  font-size: 0.85rem;
  color: #323130;
  font-weight: 500;
  white-space: nowrap;
}

.sort-select {
  padding: 6px 8px;
  border: 1px solid #c8c6c4;
  border-radius: 4px;
  background: white;
  font-size: 0.85rem;
  color: #323130;
  cursor: pointer;
  -webkit-appearance: none;
  -moz-appearance: none;
  appearance: none;
  background-image: url("data:image/svg+xml;charset=UTF-8,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3e%3cpolyline points='6 9 12 15 18 9'%3e%3c/polyline%3e%3c/svg%3e");
  background-repeat: no-repeat;
  background-position: right 8px center;
  background-size: 14px;
  flex-grow: 1;
  max-width: 150px;
}

.sort-direction-btn {
  padding: 6px 10px;
  background: #0078d4;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.85rem;
  font-weight: 500;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.sort-direction-btn:hover {
  background: #005a9e;
}

/* --- End SharePoint Sort Controls --- */

.mail-list-container {
  flex: 1;
  overflow-y: auto;
  padding-bottom: 10px;
  scroll-behavior: smooth;
  height: auto;
  min-height: 0;
}


.mail-item {
  background: white;
  border-bottom: 1px solid #f3f2f1;
  padding: 12px;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
  min-height: 80px;
}


.mail-item:hover {
  background: #e0e0e0;
}

.mail-item.selected {
  background: #deecf9;
  border-left: 4px solid #0078d4;
  box-shadow: 0 2px 4px rgba(0, 120, 212, 0.1);
}

.mail-item.has-matches {
  border-left: 3px solid #107c10;
}


.mail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}

.mail-meta {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-grow: 1;
  min-width: 0;
}

.total-revenue {
  font-size: 0.9rem;
  font-weight: 500;
  color: #1e8449;
  background-color: #e9f7ef;
  padding: 3px 8px;
  border-radius: 4px;
  white-space: nowrap;
}

.opportunity-id {
  font-size: 0.9rem;
  font-weight: 600;
  color: #0078d4;
  background: #cce7f0;
  padding: 3px 8px;
  border-radius: 4px;
}


.match-indicator {
  position: absolute;
  top: 10px;
  right: 10px;
  background: linear-gradient(135deg, #28a745, #229954);
  color: white;
  border-radius: 50%;
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.9rem;
  font-weight: 700;
  box-shadow: 0 2px 4px rgba(40, 167, 69, 0.4);
  border: 2px solid white;
}


.mail-subject h4 {
  margin: 0 0 12px 0;
  font-size: 1rem;
  font-weight: 600;
  color: #323130;
  line-height: 1.3;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-box-orient: vertical;
}

.mail-preview {
  display: flex;
  flex-direction: column;
  gap: 7px;
}

.preview-line {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  font-size: 0.9rem;
  line-height: 1;
}

.preview-line .label {
  color: #000000;
  font-weight: 500;
  min-width: 45px;
  flex-shrink: 0;
}

.preview-line .value {
  color: #323130;
  font-weight: 400;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
}

/* Right Panel */
.detail-panel {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: white;
}


.no-selection-state {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100%;
  padding: 20px;
  background-color: #f8fafc;
}

.selection-message-wrapper {
  display: flex;
  align-items: baseline;
  max-width: 450px;
  color: #667e9f;
}

.prompt-icon {
  font-size: 5rem;
  margin-right: 25px;
  flex-shrink: 0;
  line-height: 1;
}

.selection-text-content h3 {
  margin: 0 0 5px 0;
  font-size: 1.6rem;
  color: #3b4d66;
}

.selection-text-content p {
  margin: 0;
  font-size: 1rem;
  line-height: 1.5;
  color: #667e9f;
}


/* Selected header */
.selected-item-header {
  background: #f8f9fa;
  padding: 20px 24px;
  border-bottom: 1px solid #e1e5e9;
  flex-shrink: 0;
}

.selected-item-header h2 {
  margin: 0 0 10px 0;
  color: #323130;
  font-size: 1.25rem;
  font-weight: 600;
}

.item-meta {
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
}

.meta-item {
  font-size: 0.9rem;
  color: #666;
  background: #f3f2f1;
  padding: 4px 8px;
  border-radius: 4px;
}


.detail-content {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.azure-top {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px 16px;
  box-sizing: border-box;
  border-bottom: 2px solid #eef2f7;
  background: #ffffff;
  max-height: 66%;
}

.sections-container {
  flex-grow: 1;
  overflow-y: auto;
  padding: 0 30px;
  padding-top: 10px;
  display: flex;
  flex-direction: column;
  min-height: 0;
  background-color: #f8f9fa;
}

.azure-fulltable-wrapper {
  flex: 1 1 auto;
  min-height: 250px;
  overflow: hidden;
}


.similar-header-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #fff7d6;
  border-left: 4px solid #eab308;
  padding: 10px 14px;
  border-radius: 6px;
  gap: 12px;
  flex: 0 0 auto;
}

.similar-header-top h3 {
  margin: 0;
  font-size: 1rem;
  color: #a16207;
  font-weight: 600;
}

.similar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.similarity-info {
  color: #a16207;
  font-size: 0.85rem;
}

.similar-actions {
  display: flex;
  gap: 8px;
  align-items: center;
}

.azure-fulltable-wrapper :deep(.table-container),
.azure-fulltable-wrapper>>>.table-container,
.azure-fulltable-wrapper /deep/ .table-container {
  height: 100%;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

/* Sections below (matched items) */
.matched-items-section {
  flex-grow: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  margin-bottom: 24px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 12px;
  border-radius: 4px;
  margin-top: 15px;
  margin-bottom: 12px;
}

.matched-header {
  background: #f0f8f0;
  border-left: 4px solid #107c10;
}

.matched-header h3 {
  margin: 0;
  font-size: 0.9rem;
  font-weight: 600;
  color: #107c10;
}

.count-badge {
  background: #107c10;
  color: white;
  padding: 3px 8px;
  border-radius: 12px;
  font-size: 0.9rem;
  font-weight: 600;
}

.matched-items-container {
  flex-grow: 1;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding-right: 8px;
  margin-bottom: 20px;
}

.matched-azure-card {
  background: #f8fff8;
  border: 1px solid #d1fae5;
  border-radius: 8px;
  min-height: 100px;
  padding: 12px;
  transition: all 0.2s ease;
}

.matched-azure-card:hover {
  background: #f0fdf4;
  border-color: #a7f3d0;
}

.matched-azure-card .card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.similarity-score {
  padding: 3px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  color: white;
}

.similarity-score.matched {
  background: #16a34a;
}

/* --- Unmatch Button Style --- */
.unmatch-btn {
  background: #fef2f2;
  border: 1px solid #dc2626;
  color: #dc2626;
  cursor: pointer;
  font-size: 1.2rem;
  padding: 4px 10px;
  border-radius: 6px;
  line-height: 1;
  transition: all 0.2s ease;
}

.unmatch-btn:hover {
  background: #dc2626;
  color: white;
}


.matched-azure-card .card-content h4 {
  margin: 0 0 4px 0;
  font-size: 0.8rem;
  font-weight: 600;
  color: #323130;
}

.matched-azure-card .email {
  margin: 0 0 6px 0;
  font-size: 0.75rem;
  color: #0078d4;
}

.matched-azure-card .details {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.matched-azure-card .detail {
  font-size: 0.8rem;
  color: #666;
  background: #f3f2f1;
  padding: 2px 5px;
  border-radius: 3px;
}

.empty-state,
.empty-mail-list {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  text-align: center;
  color: #666;
}

.reload-btn {
  padding: 8px 16px;
  background: #0078d4;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.reload-btn:hover {
  background: #106ebe;
}

/* Footer */
.page-footer {
  background: white;
  padding: 15px 30px;
  box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.1);
  flex-shrink: 0;
}

.footer-content {
  max-width: 1400px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.match-summary {
  font-weight: 600;
  color: #1e293b;
  font-size: 1.1rem;
}

.next-btn {
  padding: 12px 24px;
  background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
  color: white;
  border: none;
  border-radius: 10px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}

.next-btn:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 15px rgba(59, 130, 246, 0.4);
}

.next-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
}

/* Scrollbars */
.mail-list-container::-webkit-scrollbar,
.matched-items-container::-webkit-scrollbar,
.sections-container::-webkit-scrollbar,
.azure-fulltable-wrapper::-webkit-scrollbar {
  width: 6px;
}

.mail-list-container::-webkit-scrollbar-thumb,
.matched-items-container::-webkit-scrollbar-thumb,
.sections-container::-webkit-scrollbar-thumb,
.azure-fulltable-wrapper::-webkit-scrollbar-thumb {
  background: #cbd5e0;
  border-radius: 3px;
}

/* Responsive */
@media (min-width: 1400px) {
  .outlook-layout {
    grid-template-columns: 320px 1fr;
    height: 92vh;
  }
}

@media (min-width: 1600px) {
  .outlook-layout {
    grid-template-columns: 320px 1fr;
    height: 94vh;
  }
}

@media (max-width: 768px) {
  .azure-top {
    height: auto;
    min-height: 200px;
  }

  .outlook-layout {
    grid-template-columns: 1fr;
    gap: 12px;
  }
}
</style>