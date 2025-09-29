<template>
  <div class="mail-card" :class="{ 'selected': selected }" @click="handleSelect">
    <div class="mail-header">
      <div class="sender-info">
        <span class="opportunity-id">{{ data.opportunityId || 'N/A' }}</span>
        <span class="date">{{ formatDate(data.registerDate) }}</span>
      </div>
      <div class="match-indicator" v-if="hasMatches">
        <span class="match-count">{{ matchCount }}</span>
      </div>
    </div>
    
    <div class="mail-subject">
      <h4>{{ data.customerName || 'Unknown Customer' }}</h4>
    </div>
    
    <div class="mail-preview">
      <div class="preview-line">
        <span class="label">Product:</span>
        <span class="value">{{ data.productName || 'N/A' }}</span>
      </div>
      <div class="preview-line">
        <span class="label">DWIN Date:</span>
        <span class="value">{{ formatDate(data.s9DWINEntryDate) || 'N/A' }}</span>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'MailCard',
  props: {
    data: {
      type: Object,
      required: true
    },
    selected: {
      type: Boolean,
      default: false
    },
    hasMatches: {
      type: Boolean,
      default: false
    },
    matchCount: {
      type: Number,
      default: 0
    }
  },
  emits: ['select'],
  methods: {
    handleSelect() {
      this.$emit('select', this.data)
    },
    
    formatDate(dateString) {
      if (!dateString) return 'N/A'
      try {
        return new Date(dateString).toLocaleDateString('th-TH', {
          day: '2-digit',
          month: 'short',
          year: 'numeric'
        })
      } catch {
        return 'N/A'
      }
    }
  }
}
</script>

<style scoped>
.mail-card {
  background: white;
  border: 1px solid #e1e5e9;
  border-radius: 4px;
  padding: 12px;
  margin-bottom: 2px;
  cursor: pointer;
  transition: all 0.2s ease;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

.mail-card:hover {
  background: #f3f2f1;
  border-color: #0078d4;
}

.mail-card.selected {
  background: #deecf9;
  border-color: #0078d4;
  border-left: 3px solid #0078d4;
}

.mail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.sender-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.opportunity-id {
  font-size: 0.8rem;
  font-weight: 600;
  color: #0078d4;
  background: #deecf9;
  padding: 2px 6px;
  border-radius: 3px;
}

.date {
  font-size: 0.9rem;
  color: #666;
}

.match-indicator {
  background: #107c10;
  color: white;
  border-radius: 50%;
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.7rem;
  font-weight: 600;
}

.mail-subject h4 {
  margin: 0 0 8px 0;
  font-size: 0.9rem;
  font-weight: 600;
  color: #323130;
  line-height: 1.2;
}

.mail-preview {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.preview-line {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.8rem;
}

.label {
  color: #666;
  font-weight: 500;
  min-width: 50px;
}

.value {
  color: #323130;
  font-weight: 400;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>