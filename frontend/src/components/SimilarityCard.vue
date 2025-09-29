<template>
  <div class="similarity-card" :class="{ 'high-similarity': similarity >= 80 }">
    <!-- ... existing header code ... -->
    
    <!-- เพิ่ม Modal สำหรับ Compare -->
    <div v-if="showCompareModal" class="modal-overlay" @click="closeCompareModal">
      <div class="compare-modal" @click.stop>
        <div class="modal-header">
          <h3>Compare Data</h3>
          <button @click="closeCompareModal" class="close-btn">✕</button>
        </div>
        
        <div class="compare-content">
          <div class="compare-columns">
            <div class="compare-column sharepoint-column">
              <h4>SharePoint Data</h4>
              <div class="compare-data">
                <div class="data-item">
                  <span class="label">Name:</span>
                  <span class="value">{{ sharepointItem.name || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Email:</span>
                  <span class="value">{{ sharepointItem.email || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Company:</span>
                  <span class="value">{{ sharepointItem.company || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Country:</span>
                  <span class="value">{{ sharepointItem.country || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Industry:</span>
                  <span class="value">{{ sharepointItem.industry || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Department:</span>
                  <span class="value">{{ sharepointItem.department || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Phone:</span>
                  <span class="value">{{ sharepointItem.phone || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Job Title:</span>
                  <span class="value">{{ sharepointItem.jobTitle || 'N/A' }}</span>
                </div>
              </div>
            </div>
            
            <div class="compare-divider">
              <div class="similarity-indicator">
                <div class="score-circle" :class="scoreClass">
                  <span>{{ Math.round(similarity) }}%</span>
                </div>
                <span class="match-label">Match Score</span>
              </div>
            </div>
            
            <div class="compare-column azure-column">
              <h4>Azure Table Data</h4>
              <div class="compare-data">
                <div class="data-item">
                  <span class="label">Name:</span>
                  <span class="value">{{ data.customerName || data.name || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Email:</span>
                  <span class="value">{{ data.customerEmail || data.email || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Company:</span>
                  <span class="value">{{ data.customerName || data.company || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Country:</span>
                  <span class="value">{{ data.country || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Industry:</span>
                  <span class="value">{{ data.customerIndustry || data.industry || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Customer Type:</span>
                  <span class="value">{{ data.typeOfCustomer || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Sales Person:</span>
                  <span class="value">{{ data.salePerson || 'N/A' }}</span>
                </div>
                <div class="data-item">
                  <span class="label">Lead Channel:</span>
                  <span class="value">{{ data.leadChannel || 'N/A' }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <div class="modal-footer">
          <button @click="closeCompareModal" class="cancel-btn">Cancel</button>
          <button @click="handleMatchFromCompare" class="match-btn-modal" :class="{ 'high-confidence': similarity >= 80 }">
            Match These Records
          </button>
        </div>
      </div>
    </div>
    
    <!-- ... existing content ... -->
    
    <div class="card-footer">
      <div class="left-actions">
        <button @click="toggleDetails" class="details-btn">
          {{ showDetails ? 'Hide Details' : 'Show Details' }}
        </button>
        <button @click="showCompare" class="compare-btn">
          Compare
        </button>
      </div>
      
      <div class="action-buttons">
        <button 
          v-if="!isMatched"
          @click="handleMatch" 
          class="match-btn"
          :class="{ 'high-confidence': similarity >= 80 }"
        >
          Match
        </button>
        <button 
          v-if="isMatched"
          @click="handleUnmatch" 
          class="unmatch-btn"
        >
          Unmatch
        </button>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'SimilarityCard',
  // ... existing props ...
  data() {
    return {
      showDetails: false,
      showCompareModal: false
    }
  },
  // ... existing computed properties ...
  methods: {
    // ... existing methods ...
    
    showCompare() {
      this.showCompareModal = true
    },
    
    closeCompareModal() {
      this.showCompareModal = false
    },
    
    handleMatchFromCompare() {
      this.handleMatch()
      this.closeCompareModal()
    }
  }
}
</script>

<style scoped>
/* เพิ่ม CSS สำหรับ Modal และ Typography */
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');

.similarity-card {
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  /* ... existing styles ... */
}

.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 15px;
  padding-top: 15px;
  border-top: 1px solid #f1f5f9;
}

.left-actions {
  display: flex;
  gap: 10px;
}

.compare-btn {
  padding: 6px 12px;
  background: #f0f9ff;
  border: 1px solid #0ea5e9;
  border-radius: 6px;
  color: #0369a1;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
}

.compare-btn:hover {
  background: #e0f2fe;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.compare-modal {
  background: white;
  border-radius: 16px;
  max-width: 1000px;
  width: 95%;
  max-height: 90vh;
  overflow: hidden;
  box-shadow: 0 25px 50px rgba(0, 0, 0, 0.25);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 24px;
  border-bottom: 1px solid #f1f5f9;
  background: #fafbfc;
}

.modal-header h3 {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: #1e293b;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #64748b;
  padding: 8px;
  border-radius: 6px;
  transition: all 0.2s ease;
}

.close-btn:hover {
  background: #f1f5f9;
  color: #334155;
}

.compare-content {
  padding: 24px;
  max-height: 60vh;
  overflow-y: auto;
}

.compare-columns {
  display: grid;
  grid-template-columns: 1fr 120px 1fr;
  gap: 24px;
  align-items: start;
}

.compare-column {
  background: #fafbfc;
  border-radius: 12px;
  padding: 20px;
  border: 2px solid #f1f5f9;
}

.compare-column h4 {
  margin: 0 0 16px 0;
  font-size: 1.1rem;
  font-weight: 600;
  text-align: center;
  padding-bottom: 12px;
  border-bottom: 2px solid #e2e8f0;
}

.sharepoint-column {
  border-color: #dbeafe;
}

.sharepoint-column h4 {
  color: #1d4ed8;
  border-bottom-color: #3b82f6;
}

.azure-column {
  border-color: #e0f2fe;
}

.azure-column h4 {
  color: #0369a1;
  border-bottom-color: #0ea5e9;
}

.compare-data {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.data-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 12px;
  background: white;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
}

.data-item .label {
  font-size: 0.8rem;
  font-weight: 600;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.data-item .value {
  font-size: 0.95rem;
  font-weight: 500;
  color: #1e293b;
  word-break: break-word;
}

.compare-divider {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 20px 0;
}

.similarity-indicator {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}

.match-label {
  font-size: 0.8rem;
  color: #64748b;
  font-weight: 500;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 20px 24px;
  border-top: 1px solid #f1f5f9;
  background: #fafbfc;
}

.cancel-btn {
  padding: 10px 20px;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  color: #6b7280;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.cancel-btn:hover {
  background: #f9fafb;
  border-color: #9ca3af;
}

.match-btn-modal {
  padding: 10px 20px;
  background: #3b82f6;
  color: white;
  border: none;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.match-btn-modal:hover {
  background: #2563eb;
}

.match-btn-modal.high-confidence {
  background: #10b981;
}

.match-btn-modal.high-confidence:hover {
  background: #059669;
}

@media (max-width: 768px) {
  .compare-columns {
    grid-template-columns: 1fr;
    gap: 16px;
  }
  
  .compare-divider {
    order: -1;
    padding: 16px;
    background: #f8fafc;
    border-radius: 8px;
  }
}
</style>