<template>
  <div class="matched-items-section">
    <div class="section-header matched-header">
      <h3>Matched Records</h3>
      <span class="count-badge">{{ matchedItems.length }}</span>
    </div>
    
    <div class="matched-items-container" v-if="matchedItems.length > 0">
      <div 
        v-for="item in matchedItems" 
        :key="item.id"
        class="matched-azure-card"
      >
        <div class="card-header">
          <div class="similarity-score matched">
            {{ Math.round(item.similarity) }}%
          </div>
          <button @click="handleUnmatch(item.id)" class="unmatch-btn" title="Unmatch">
            ✕
          </button>
        </div>
        
        <div class="card-content">
          <h4>{{ item.customerName || item.name || 'Unknown' }}</h4>
          <p class="email">{{ item.customerEmail || item.email || 'N/A' }}</p>
          <div class="details">
            <span class="detail">{{ item.country || 'N/A' }}</span>
            <span class="detail">{{ item.customerIndustry || 'N/A' }}</span>
          </div>
        </div>
      </div>
    </div>
    
    <div v-else class="empty-matched">
      <p>No matched records yet</p>
    </div>
  </div>
</template>

<script>
export default {
  name: 'MatchedItemsSection',
  props: {
    matchedItems: {
      type: Array,
      default: () => []
    }
  },
  emits: ['unmatch'],
  methods: {
    handleUnmatch(azureId) {
      this.$emit('unmatch', azureId)
    }
  }
}
</script>

<style scoped>
.matched-items-section {
  margin-bottom: 20px;
}

.section-header {
  display: flex;
  justify-content: between;
  align-items: center;
  padding: 12px 16px;
  background: #e8f5e8;
  border-radius: 4px;
  margin-bottom: 8px;
}

.matched-header {
  background: #e8f5e8;
  border-left: 4px solid #107c10;
}

.section-header h3 {
  margin: 0;
  font-size: 0.9rem;
  font-weight: 600;
  color: #107c10;
}

.count-badge {
  background: #107c10;
  color: white;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 600;
}

.matched-items-container {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 200px;
  overflow-y: auto;
}

.matched-azure-card {
  background: #f0f8f0;
  border: 1px solid #c6e6c6;
  border-radius: 4px;
  padding: 10px;
  transition: all 0.2s ease;
}

.matched-azure-card:hover {
  background: #e8f5e8;
  border-color: #107c10;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.similarity-score {
  padding: 2px 6px;
  border-radius: 3px;
  font-size: 0.75rem;
  font-weight: 600;
  color: white;
}

.similarity-score.matched {
  background: #107c10;
}

.unmatch-btn {
  background: none;
  border: none;
  color: #d13438;
  cursor: pointer;
  font-size: 0.8rem;
  padding: 2px 4px;
  border-radius: 2px;
}

.unmatch-btn:hover {
  background: #fde7e9;
}

.card-content h4 {
  margin: 0 0 4px 0;
  font-size: 0.85rem;
  font-weight: 600;
  color: #323130;
}

.email {
  margin: 0 0 6px 0;
  font-size: 0.8rem;
  color: #0078d4;
}

.details {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.detail {
  font-size: 0.75rem;
  color: #666;
  background: #f3f2f1;
  padding: 2px 6px;
  border-radius: 2px;
}

.empty-matched {
  text-align: center;
  padding: 20px;
  color: #666;
  font-size: 0.85rem;
}
</style>