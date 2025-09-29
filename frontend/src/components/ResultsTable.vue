<template>
  <div class="results-table">
    <div v-if="loading" class="loading-container">
      <LoadingSpinner />
      <p>Loading results...</p>
    </div>
    
    <div v-else-if="data.length === 0" class="empty-state">
      <div class="empty-icon">📊</div>
      <h3>No results to display</h3>
      <p>Go back to the matching page to create some matches first.</p>
      <button @click="$emit('edit')" class="empty-action-btn">
        Start Matching
      </button>
    </div>
    
    <div v-else class="table-container">
      <div class="table-wrapper">
        <table class="results-data-table">
          <thead>
            <tr>
              <th class="sticky-col">Name</th>
              <th>Email</th>
              <th>Company</th>
              <th>Country</th>
              <th>Industry</th>
              <th>Customer Type</th>
              <th>Sales Person</th>
              <th>Lead Channel</th>
              <th>Similarity</th>
              <th>Match Type</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr 
              v-for="(item, index) in data" 
              :key="`result-${index}`"
              class="data-row"
              :class="{ 'high-similarity': (item.similarity || 0) >= 80 }"
            >
              <td class="sticky-col name-cell">
                <div class="name-content">
                  <h4>{{ item.name || item.customerName || 'Unknown' }}</h4>
                  <span class="record-id">ID: {{ item.id || 'N/A' }}</span>
                </div>
              </td>
              
              <td class="email-cell">
                <span class="email-value">
                  {{ item.email || item.customerEmail || 'N/A' }}
                </span>
              </td>
              
              <td class="company-cell">
                {{ item.company || item.customerName || 'N/A' }}
              </td>
              
              <td class="country-cell">
                <span class="country-flag">
                  {{ getCountryFlag(item.country) }}
                </span>
                {{ item.country || 'N/A' }}
              </td>
              
              <td class="industry-cell">
                {{ item.industry || item.customerIndustry || 'N/A' }}
              </td>
              
              <td class="type-cell">
                {{ item.typeOfCustomer || item.customerType || 'N/A' }}
              </td>
              
              <td class="sales-cell">
                {{ item.salePerson || 'N/A' }}
              </td>
              
              <td class="channel-cell">
                {{ item.leadChannel || 'N/A' }}
              </td>
              
              <td class="similarity-cell">
                <div class="similarity-container">
                  <div 
                    class="similarity-progress" 
                    :class="getSimilarityClass(item.similarity || 0)"
                  >
                    <div 
                      class="progress-fill" 
                      :style="{ width: (item.similarity || 0) + '%' }"
                    ></div>
                  </div>
                  <span class="similarity-text">
                    {{ Math.round(item.similarity || 0) }}%
                  </span>
                </div>
              </td>
              
              <td class="match-type-cell">
                <span 
                  class="match-badge" 
                  :class="item.matchType || 'manual'"
                >
                  {{ formatMatchType(item.matchType || 'manual') }}
                </span>
              </td>
              
              <td class="actions-cell">
                <div class="action-buttons">
                  <button 
                    @click="viewDetails(item)"
                    class="view-details-btn"
                    title="View Details"
                  >
                    👁️
                  </button>
                  <button 
                    @click="$emit('edit')"
                    class="edit-btn"
                    title="Edit Match"
                  >
                    ✏️
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      
      <!-- Pagination -->
      <div v-if="data.length > itemsPerPage" class="pagination">
        <button 
          @click="currentPage = Math.max(1, currentPage - 1)"
          :disabled="currentPage === 1"
          class="pagination-btn"
        >
          ← Previous
        </button>
        
        <div class="page-info">
          Page {{ currentPage }} of {{ totalPages }}
          <span class="items-info">({{ data.length }} items)</span>
        </div>
        
        <button 
          @click="currentPage = Math.min(totalPages, currentPage + 1)"
          :disabled="currentPage === totalPages"
          class="pagination-btn"
        >
          Next →
        </button>
      </div>
    </div>

    <!-- Details Modal -->
    <div v-if="showDetailsModal" class="modal-overlay" @click="closeDetailsModal">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>Record Details</h3>
          <button @click="closeDetailsModal" class="close-btn">✕</button>
        </div>
        
        <div class="modal-body" v-if="selectedItem">
          <div class="details-tabs">
            <button 
              @click="activeTab = 'merged'"
              class="tab-btn"
              :class="{ active: activeTab === 'merged' }"
            >
              Merged Data
            </button>
            <button 
              @click="activeTab = 'original'"
              class="tab-btn"
              :class="{ active: activeTab === 'original' }"
            >
              Original Sources
            </button>
          </div>
          
          <div v-if="activeTab === 'merged'" class="tab-content">
            <div class="details-grid">
              <div 
                v-for="(value, key) in selectedItem" 
                :key="key"
                class="detail-item"
              >
                <span class="detail-label">{{ formatLabel(key) }}:</span>
                <span class="detail-value">{{ formatValue(value) }}</span>
              </div>
            </div>
          </div>
          
          <div v-if="activeTab === 'original'" class="tab-content">
            <div class="original-sources">
              <div class="source-section">
                <h4>SharePoint Source</h4>
                <p class="source-note">Original data from SharePoint list</p>
                <div class="source-data">
                  <code>{{ JSON.stringify(selectedItem.sharePointOriginal || {}, null, 2) }}</code>
                </div>
              </div>
              
              <div class="source-section">
                <h4>Azure Table Source</h4>
                <p class="source-note">Original data from Azure Table</p>
                <div class="source-data">
                 <code>{{ JSON.stringify(selectedItem.azureOriginal || {}, null, 2) }}</code>
               </div>
             </div>
           </div>
         </div>
       </div>
     </div>
   </div>
 </div>
</template>

<script>
import LoadingSpinner from './LoadingSpinner.vue'

export default {
 name: 'ResultsTable',
 components: {
   LoadingSpinner
 },
 props: {
   data: {
     type: Array,
     required: true
   },
   loading: {
     type: Boolean,
     default: false
   }
 },
 emits: ['edit'],
 data() {
   return {
     currentPage: 1,
     itemsPerPage: 50,
     showDetailsModal: false,
     selectedItem: null,
     activeTab: 'merged'
   }
 },
 computed: {
   totalPages() {
     return Math.ceil(this.data.length / this.itemsPerPage)
   },
   
   paginatedData() {
     const start = (this.currentPage - 1) * this.itemsPerPage
     const end = start + this.itemsPerPage
     return this.data.slice(start, end)
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
       auto: 'Auto',
       ai: 'AI'
     }
     return types[type] || type
   },
   
   formatLabel(key) {
     const labels = {
       name: 'Name',
       customerName: 'Customer Name',
       email: 'Email',
       customerEmail: 'Customer Email',
       company: 'Company',
       country: 'Country',
       industry: 'Industry',
       customerIndustry: 'Customer Industry',
       typeOfCustomer: 'Customer Type',
       salePerson: 'Sales Person',
       leadChannel: 'Lead Channel',
       website: 'Website',
       contactPerson: 'Contact Person',
       productInterest: 'Product Interest',
       department: 'Department',
       phone: 'Phone',
       jobTitle: 'Job Title',
       matchType: 'Match Type',
       similarity: 'Similarity Score',
       matchTimestamp: 'Match Date'
     }
     return labels[key] || key.charAt(0).toUpperCase() + key.slice(1)
   },
   
   formatValue(value) {
     if (value === null || value === undefined) return 'N/A'
     if (typeof value === 'boolean') return value ? 'Yes' : 'No'
     if (typeof value === 'number') return value.toString()
     if (typeof value === 'object') return JSON.stringify(value)
     return value.toString()
   },
   
   getCountryFlag(country) {
     const flags = {
       'USA': '🇺🇸',
       'United States': '🇺🇸',
       'Canada': '🇨🇦',
       'UK': '🇬🇧',
       'United Kingdom': '🇬🇧',
       'Germany': '🇩🇪',
       'France': '🇫🇷',
       'Japan': '🇯🇵',
       'China': '🇨🇳',
       'Australia': '🇦🇺',
       'Singapore': '🇸🇬',
       'Thailand': '🇹🇭'
     }
     return flags[country] || '🌍'
   },
   
   viewDetails(item) {
     this.selectedItem = item
     this.showDetailsModal = true
     this.activeTab = 'merged'
   },
   
   closeDetailsModal() {
     this.showDetailsModal = false
     this.selectedItem = null
   }
 }
}
</script>

<style scoped>
.results-table {
 width: 100%;
}

.loading-container {
 display: flex;
 flex-direction: column;
 align-items: center;
 justify-content: center;
 padding: 60px 20px;
 color: #666;
}

.loading-container p {
 margin-top: 20px;
 font-size: 1rem;
}

.empty-state {
 text-align: center;
 padding: 60px 20px;
 color: #666;
}

.empty-icon {
 font-size: 4rem;
 margin-bottom: 20px;
}

.empty-state h3 {
 font-size: 1.5rem;
 color: #333;
 margin: 0 0 15px 0;
}

.empty-state p {
 font-size: 1rem;
 margin: 0 0 25px 0;
 max-width: 400px;
 margin-left: auto;
 margin-right: auto;
 line-height: 1.6;
}

.empty-action-btn {
 padding: 12px 24px;
 background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
 color: white;
 border: none;
 border-radius: 8px;
 font-size: 1rem;
 font-weight: 600;
 cursor: pointer;
 transition: all 0.3s ease;
}

.empty-action-btn:hover {
 transform: translateY(-2px);
 box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
}

.table-container {
 background: white;
 border-radius: 12px;
 overflow: hidden;
 box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.table-wrapper {
 overflow-x: auto;
 max-height: 600px;
 overflow-y: auto;
}

.results-data-table {
 width: 100%;
 min-width: 1200px;
 border-collapse: collapse;
}

.results-data-table thead th {
 background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
 color: white;
 padding: 15px 12px;
 text-align: left;
 font-weight: 600;
 font-size: 0.9rem;
 position: sticky;
 top: 0;
 z-index: 10;
}

.sticky-col {
 position: sticky;
 left: 0;
 background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
 z-index: 11;
}

.data-row {
 border-bottom: 1px solid #f1f5f9;
 transition: background-color 0.3s ease;
}

.data-row:hover {
 background: #f8f9fa;
}

.data-row.high-similarity {
 background: linear-gradient(90deg, #e6ffed 0%, transparent 100%);
}

.results-data-table td {
 padding: 12px;
 vertical-align: top;
 font-size: 0.9rem;
}

.name-cell {
 position: sticky;
 left: 0;
 background: white;
 min-width: 180px;
 z-index: 5;
}

.data-row:hover .name-cell {
 background: #f8f9fa;
}

.data-row.high-similarity .name-cell {
 background: #e6ffed;
}

.name-content {
 display: flex;
 flex-direction: column;
 gap: 4px;
}

.name-content h4 {
 font-size: 0.95rem;
 font-weight: 600;
 color: #333;
 margin: 0;
}

.record-id {
 font-size: 0.75rem;
 color: #999;
 font-family: monospace;
}

.email-cell {
 min-width: 200px;
}

.email-value {
 color: #0066cc;
 text-decoration: none;
}

.country-cell {
 min-width: 120px;
}

.country-flag {
 margin-right: 8px;
}

.similarity-cell {
 min-width: 120px;
}

.similarity-container {
 display: flex;
 flex-direction: column;
 gap: 5px;
}

.similarity-progress {
 width: 100%;
 height: 6px;
 background: #f1f5f9;
 border-radius: 3px;
 overflow: hidden;
}

.progress-fill {
 height: 100%;
 border-radius: 3px;
 transition: width 0.3s ease;
}

.similarity-progress.excellent .progress-fill {
 background: linear-gradient(90deg, #38a169 0%, #48bb78 100%);
}

.similarity-progress.good .progress-fill {
 background: linear-gradient(90deg, #3182ce 0%, #4299e1 100%);
}

.similarity-progress.medium .progress-fill {
 background: linear-gradient(90deg, #d69e2e 0%, #ecc94b 100%);
}

.similarity-progress.low .progress-fill {
 background: linear-gradient(90deg, #e53e3e 0%, #fc8181 100%);
}

.similarity-text {
 font-size: 0.8rem;
 font-weight: 600;
 color: #333;
}

.match-badge {
 display: inline-block;
 padding: 4px 8px;
 border-radius: 4px;
 font-size: 0.75rem;
 font-weight: 600;
 text-transform: uppercase;
}

.match-badge.manual {
 background: #e3f2fd;
 color: #1565c0;
}

.match-badge.auto {
 background: #e8f5e8;
 color: #2e7d32;
}

.match-badge.ai {
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

.view-details-btn, .edit-btn {
 padding: 6px;
 border: none;
 border-radius: 4px;
 cursor: pointer;
 font-size: 0.9rem;
 transition: all 0.3s ease;
}

.view-details-btn {
 background: #f0f9ff;
 color: #0369a1;
}

.view-details-btn:hover {
 background: #e0f2fe;
}

.edit-btn {
 background: #fef3c7;
 color: #d97706;
}

.edit-btn:hover {
 background: #fde68a;
}

.pagination {
 display: flex;
 justify-content: space-between;
 align-items: center;
 padding: 20px;
 border-top: 1px solid #f1f5f9;
 background: #fafbfc;
}

.pagination-btn {
 padding: 8px 16px;
 background: #667eea;
 color: white;
 border: none;
 border-radius: 6px;
 cursor: pointer;
 transition: all 0.3s ease;
}

.pagination-btn:hover:not(:disabled) {
 background: #5a67d8;
}

.pagination-btn:disabled {
 opacity: 0.5;
 cursor: not-allowed;
}

.page-info {
 font-weight: 600;
 color: #333;
}

.items-info {
 color: #666;
 font-weight: normal;
 font-size: 0.9rem;
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
 max-width: 800px;
 width: 90%;
 max-height: 80vh;
 overflow: hidden;
 display: flex;
 flex-direction: column;
}

.modal-header {
 display: flex;
 justify-content: space-between;
 align-items: center;
 padding: 20px;
 border-bottom: 1px solid #f1f5f9;
 flex-shrink: 0;
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
 flex: 1;
 overflow: auto;
}

.details-tabs {
 display: flex;
 border-bottom: 1px solid #f1f5f9;
}

.tab-btn {
 flex: 1;
 padding: 15px;
 background: none;
 border: none;
 font-size: 0.9rem;
 font-weight: 600;
 cursor: pointer;
 color: #666;
 transition: all 0.3s ease;
}

.tab-btn.active {
 color: #667eea;
 background: #f8f9fa;
 border-bottom: 2px solid #667eea;
}

.tab-btn:hover:not(.active) {
 background: #f1f5f9;
}

.tab-content {
 padding: 20px;
}

.details-grid {
 display: grid;
 grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
 gap: 15px;
}

.detail-item {
 display: flex;
 flex-direction: column;
 gap: 5px;
 padding: 10px;
 background: #f8f9fa;
 border-radius: 6px;
}

.detail-label {
 font-size: 0.8rem;
 color: #666;
 font-weight: 600;
}

.detail-value {
 font-size: 0.9rem;
 color: #333;
 word-break: break-word;
}

.original-sources {
 display: flex;
 flex-direction: column;
 gap: 20px;
}

.source-section {
 background: #f8f9fa;
 border-radius: 8px;
 padding: 15px;
}

.source-section h4 {
 margin: 0 0 10px 0;
 color: #333;
}

.source-note {
 margin: 0 0 15px 0;
 color: #666;
 font-size: 0.9rem;
}

.source-data {
 background: #2d3748;
 color: #e2e8f0;
 border-radius: 6px;
 padding: 15px;
 overflow-x: auto;
}

.source-data code {
 font-family: 'Monaco', 'Menlo', monospace;
 font-size: 0.8rem;
 line-height: 1.5;
 white-space: pre;
}

@media (max-width: 768px) {
 .table-wrapper {
   max-height: 400px;
 }
 
 .results-data-table {
   min-width: 800px;
 }
 
 .pagination {
   flex-direction: column;
   gap: 15px;
   text-align: center;
 }
 
 .details-grid {
   grid-template-columns: 1fr;
 }
 
 .original-sources {
   gap: 15px;
 }
}
</style>