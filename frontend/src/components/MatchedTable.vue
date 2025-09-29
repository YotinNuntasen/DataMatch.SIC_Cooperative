<template>

  <div class="matched-table">
    <div class="table-container">
      <table class="matches-table">
        <thead>
          <tr>
            <th>SharePoint Data</th>
            <th>Azure Table Data</th>
            <th>Similarity</th>
            <th>Match Type</th>
            <th>Actions</th>
          </tr>
        </thead> 
        <tbody>
          
          <template v-for="pair in matchedPairs" :key="pair.sharepoint?.id + '-' + pair.azure?.id">
            <tr v-if="pair && pair.sharepoint?.id && pair.azure?.id" class="match-row">

              <td class="sharepoint-cell">
                <div class="cell-content">
                  <div class="main-info">
                    <h4 class="item-name">{{ pair.sharepoint?.OpportunityName || 'Unknown Opportunity' }}</h4>
                    <p class="item-email">{{ pair.sharepoint?.CustomerNameCustomerName || 'No Customer' }}</p>
                  </div>
                  <div class="secondary-info">
                    <span class="info-item">{{ pair.sharepoint?.CaelnCharge || 'N/A' }}</span>
                    <span class="info-item">{{ pair.sharepoint?.RegisterDate ? new
                      Date(pair.sharepoint.RegisterDate).toLocaleDateString() : 'N/A' }}</span>
                  </div>
                </div>
              </td>

         
              <td class="azure-cell">
                <div class="cell-content">
                  <div class="main-info">
                    <h4 class="item-name">{{ pair.azure?.customerName || pair.azure?.name || 'Unknown' }}</h4>
                    <p class="item-email">{{ pair.azure?.customerEmail || pair.azure?.email || 'No email' }}</p>
                  </div>
                  <div class="secondary-info">
                    <span class="info-item">{{ pair.azure?.customerBusinessType || 'N/A' }}</span>
                    <span class="info-item">{{ pair.azure?.country || 'N/A' }}</span>
                  </div>
                </div>
              </td>

              <!-- Similarity Column (ปลอดภัยแล้ว) -->
              <td class="similarity-cell">
                <div class="similarity-indicator">
                  <div class="similarity-bar" :style="{ width: pair.similarity + '%' }"
                    :class="getSimilarityClass(pair.similarity)"></div>
                  <span class="similarity-text">{{ Math.round(pair.similarity) }}%</span>
                </div>
              </td>

              <!-- Match Type Column (ปลอดภัยแล้ว) -->
              <td class="type-cell">
                <span class="match-type-badge" :class="pair.matchType">
                  {{ formatMatchType(pair.matchType) }}
                </span>
              </td>

            
              <td class="actions-cell">
                <div class="action-buttons">
                  <button @click="viewDetails(pair)" class="view-btn" title="View Details">
                    👁️
                  </button>
                  <button @click="handleUnmatch(pair.sharepoint?.id, pair.azure?.id)" class="unmatch-btn"
                    title="Unmatch">
                    ✕
                  </button>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <!-- Empty State (ถูกต้องแล้ว) -->
    <div v-if="!matchedPairs || matchedPairs.length === 0" class="empty-state">
      <div class="empty-icon">📝</div>
      <h3>No matches yet</h3>
      <p>Start by selecting a SharePoint item and matching it with similar Azure Table records.</p>
    </div>

    <div v-if="showDetailsModal" class="modal-overlay" @click="closeModal">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>Match Details</h3>
          <button @click="closeModal" class="close-btn">✕</button>
        </div>

        <div class="modal-body" v-if="selectedPair
">
          <div class="details-comparison">
            <div class="comparison-column">
              <h4>SharePoint Data</h4>
              <div class="data-details">
                <div v-for="(value, key) in selectedPair
.sharepoint" :key="'sp-' + key" class="detail-row">
                  <span class="detail-label">{{ formatLabel(key) }}:</span>
                  <span class="detail-value">{{ value || 'N/A' }}</span>
                </div>
              </div>
            </div>

            <div class="comparison-column">
              <h4>Azure Table Data</h4>
              <div class="data-details">
                <div v-for="(value, key) in selectedPair
.azure" :key="'az-' + key" class="detail-row">
                  <span class="detail-label">{{ formatLabel(key) }}:</span>
                  <span class="detail-value">{{ value || 'N/A' }}</span>
                </div>
              </div>
            </div>
          </div>

          <div class="match-info">
            <div class="info-item">
              <span class="info-label">Similarity Score:</span>
              <span class="info-value">{{ Math.round(selectedPair
.similarity) }}%</span>
            </div>
            <div class="info-item">
              <span class="info-label">Match Type:</span>
              <span class="info-value">{{ formatMatchType(selectedPair
.matchType) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">Matched On:</span>
              <span class="info-value">{{ formatDate(selectedPair
.timestamp) }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'MatchedTable',
  props: {
    matchedPairs: {
      type: Array,
      required: true
    }
  },
  emits: ['unmatch'],
  data() {
    return {
      showDetailsModal: false,
      selectedPair
: null
    }
  },
  methods: {
    getSimilarityClass(similarity) {
      if (similarity >= 90) return 'excellent'
      if (similarity >= 80) return 'good'
      if (similarity >= 60) return 'medium'
      return 'low'
    },

    formatMatchType(type) {
      const types = {
        manual: 'Manual',
        auto: 'Automatic',
        ai: 'AI Suggested'
      }
      return types[type] || type
    },

    formatLabel(key) {
      const labels = {
        // --- SharePoint Labels ---
        'OpportunityId': 'Opp. ID',
        'OpportunityName': 'Opp. Name',
        'ExpectedRevenueUSD': 'Revenue (USD)',
        'PipelineStage': 'Stage',
        'RegisterDate': 'Register Date',
        'CaelnCharge': 'In Charge',
        'CustomerNameSalePersonCode': 'Sale Code',
        'CustomerNameCustomerName': 'Customer',
        'TargetMassProductionDate': 'MP Date',
        'S2ContactEntryDate': 'Contact Date',
        'S3QualifiedEntryDate': 'Qualified Date',
        'S4NDAEntryDate': 'NDA Date',
        'S5SampleEntryDate': 'Sample Date',
        'S6EvalEntryDate': 'Eval Date',
        'S7DIEntryDate': 'DI Date',
        'S8PreProEntryDate': 'Pre-Pro Date',
        'S9DWINEntryDate': 'DWIN Date',
        'FilterTag': 'Tag',

        // --- Azure Labels ---
        'name': 'Name',
        'email': 'Email',
        'company': 'Company',
        'country': 'Country',
        'customerName': 'Customer Name',
        'customerEmail': 'Customer Email',
        'customerBusinessType': 'Business Type',
        'customerIndustry': 'Industry',
        'typeOfCustomer': 'Customer Type',
        'salePerson': 'Sales Person',
        'leadChannel': 'Lead Channel',
        'website': 'Website',
        'contactPerson': 'Contact Person',
        'productInterest': 'Product Interest',
        'department': 'Department',
        'phone': 'Phone',
        'jobTitle': 'Job Title',

        'id': 'ID'
      };

      // ทำให้ key ที่ยาวๆ น่าอ่านขึ้นก่อนส่งไปเทียบ
      const formattedKey = key.replace(/([A-Z])/g, ' $1').trim();
      return labels[key] || formattedKey;
    },

    formatDate(timestamp) {
      if (!timestamp) return 'N/A'
      return new Date(timestamp).toLocaleString()
    },

    handleUnmatch(sharepointId, azureId) {
      this.$emit('unmatch', { sharepointId, azureId });
    },

    viewDetails(pair) {
      this.selectedPair
 = pair
      this.showDetailsModal = true
    },

    closeModal() {
      this.showDetailsModal = false
      this.selectedPair
 = null
    }
  }
}
</script>

<style scoped>
/* มาตรฐาน CSS ทั้งหมดของคุณ */
.matched-table {
  width: 100%;
}

.table-container {
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.matches-table {
  width: 100%;
  border-collapse: collapse;
}

.matches-table thead th {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 15px 12px;
  text-align: left;
  font-weight: 600;
  font-size: 0.9rem;
}

.match-row {
  border-bottom: 1px solid #f1f5f9;
  transition: background-color 0.3s ease;
}

.match-row:hover {
  background: #f8f9fa;
}

.match-row:last-child {
  border-bottom: none;
}

.matches-table td {
  padding: 15px 12px;
  vertical-align: top;
}

.cell-content {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.main-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.item-name {
  font-size: 0.95rem;
  font-weight: 600;
  color: #333;
  margin: 0;
}

.item-email {
  font-size: 0.85rem;
  color: #666;
  margin: 0;
}

.secondary-info {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.info-item {
  font-size: 0.8rem;
  color: #888;
}

.sharepoint-cell .item-name {
  color: #0066cc;
}

.azure-cell .item-name {
  color: #0277bd;
}

.similarity-cell {
  min-width: 120px;
}

.similarity-indicator {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.similarity-bar {
  height: 8px;
  border-radius: 4px;
  transition: all 0.3s ease;
}

.similarity-bar.excellent {
  background: linear-gradient(90deg, #38a169 0%, #48bb78 100%);
}

.similarity-bar.good {
  background: linear-gradient(90deg, #3182ce 0%, #4299e1 100%);
}

.similarity-bar.medium {
  background: linear-gradient(90deg, #d69e2e 0%, #ecc94b 100%);
}

.similarity-bar.low {
  background: linear-gradient(90deg, #e53e3e 0%, #fc8181 100%);
}

.similarity-text {
  font-size: 0.85rem;
  font-weight: 600;
  color: #333;
}

.type-cell {
  min-width: 100px;
}

.match-type-badge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
}

.match-type-badge.manual {
  background: #e3f2fd;
  color: #1565c0;
}

.match-type-badge.auto {
  background: #e8f5e8;
  color: #2e7d32;
}

.match-type-badge.ai {
  background: #f3e5f5;
  color: #7b1fa2;
}

.actions-cell {
  min-width: 80px;
}

.action-buttons {
  display: flex;
  gap: 5px;
}

.view-btn,
.unmatch-btn {
  padding: 6px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: all 0.3s ease;
}

.view-btn {
  background: #f0f9ff;
  color: #0369a1;
}

.view-btn:hover {
  background: #e0f2fe;
}

.unmatch-btn {
  background: #fef2f2;
  color: #dc2626;
}

.unmatch-btn:hover {
  background: #fee2e2;
}

.empty-state {
  text-align: center;
  padding: 40px 20px;
  color: #666;
}

.empty-icon {
  font-size: 3rem;
  margin-bottom: 15px;
}

.empty-state h3 {
  font-size: 1.2rem;
  color: #333;
  margin: 0 0 10px 0;
}

.empty-state p {
  font-size: 0.95rem;
  max-width: 400px;
  margin: 0 auto;
  line-height: 1.5;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 12px;
  max-width: 1000px;
  width: 95%;
  max-height: 80vh;
  overflow: auto;
  display: flex;
  flex-direction: column;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #f1f5f9;
}

.modal-header h3 {
  margin: 0;
  color: #333;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.2rem;
  cursor: pointer;
  color: #666;
  padding: 5px;
}

.close-btn:hover {
  color: #333;
}

.modal-body {
  padding: 20px;
}

.details-comparison {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 30px;
  margin-bottom: 20px;
}

.comparison-column h4 {
  margin: 0 0 15px 0;
  color: #333;
  font-size: 1.1rem;
  padding-bottom: 10px;
}

.data-details {
  display: flex;
  flex-direction: column;
  gap: 12px;
}


.detail-row {
  display: grid;
  grid-template-columns: 150px 1fr;
  gap: 100px;
  align-items: start;
}

.detail-label {
  font-size: 0.85rem;
  color: #4a5568;
  font-weight: 500;
  text-align: left;
  white-space: nowrap;
}

.detail-value {
  font-size: 0.9rem;
  color: #333;
  overflow-wrap: break-word;
  word-break: break-word;
}

.match-info {
  display: flex;
  justify-content: space-around;
  padding: 15px;
  background: #f8f9fa;
  border-radius: 8px;
}

.info-item {
  text-align: center;
}


.info-label {
  display: block;
  font-size: 0.8rem;
  color: #666;
  margin-bottom: 5px;
}

.info-value {
  display: block;
  font-size: 0.95rem;
  font-weight: 600;
  color: #333;
}

@media (max-width: 768px) {
  .table-container {
    overflow-x: auto;
  }

  .matches-table {
    min-width: 700px;
  }

  .details-comparison {
    grid-template-columns: 1fr;
  }

  .match-info {
    flex-direction: column;
    gap: 15px;
  }
}
</style>