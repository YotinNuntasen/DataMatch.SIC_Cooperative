<template>
  <div 
    class="data-card" 
    :class="[
      `data-card--${layout}`,
      `data-card--${variant}`,
      { 
        'data-card--clickable': clickable,
        'data-card--selected': selected,
        'data-card--loading': loading,
        'data-card--compact': compact
      }
    ]"
    @click="handleClick"
  >
    <!-- Loading State -->
    <div v-if="loading" class="data-card__loading">
      <div class="data-card__spinner"></div>
      <span>Loading...</span>
    </div>

    <!-- Normal Content -->
    <template v-else>
      <!-- Header Section -->
      <header v-if="showHeader" class="data-card__header">
        <div class="data-card__title-section">
          <h3 v-if="title" class="data-card__title">{{ title }}</h3>
          <p v-if="subtitle" class="data-card__subtitle">{{ subtitle }}</p>
        </div>
        
        <div v-if="$slots.actions || showActions" class="data-card__actions">
          <slot name="actions">
            <button 
              v-if="showEditButton"
              @click.stop="$emit('edit', data)"
              class="data-card__action-btn data-card__action-btn--edit"
              :title="editButtonText"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" stroke="currentColor" stroke-width="2"/>
                <path d="m18.5 2.5 3 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2"/>
              </svg>
            </button>
            
            <button 
              v-if="showDeleteButton"
              @click.stop="$emit('delete', data)"
              class="data-card__action-btn data-card__action-btn--delete"
              :title="deleteButtonText"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                <path d="m3 6 18 0M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2m3 0v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6h14zM10 11l0 6M14 11l0 6" stroke="currentColor" stroke-width="2"/>
              </svg>
            </button>
            
            <button 
              v-if="showMoreButton"
              @click.stop="$emit('more', data)"
              class="data-card__action-btn"
              :title="moreButtonText"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                <circle cx="12" cy="12" r="1" stroke="currentColor" stroke-width="2"/>
                <circle cx="19" cy="12" r="1" stroke="currentColor" stroke-width="2"/>
                <circle cx="5" cy="12" r="1" stroke="currentColor" stroke-width="2"/>
              </svg>
            </button>
          </slot>
        </div>
      </header>

      <!-- Status/Badge Section -->
      <div v-if="status || badges.length > 0" class="data-card__status-section">
        <span v-if="status" class="data-card__status" :class="`data-card__status--${status.type}`">
          {{ status.text }}
        </span>
        
        <div v-if="badges.length > 0" class="data-card__badges">
          <span 
            v-for="badge in badges" 
            :key="badge.text"
            class="data-card__badge"
            :class="badge.class"
          >
            {{ badge.text }}
          </span>
        </div>
      </div>

      <!-- Content Section -->
      <main class="data-card__content">
        <slot name="content">
          <!-- Row Layout (for Azure data style) -->
          <div v-if="layout === 'row'" class="data-card__row-content">
            <div v-for="field in displayFields" :key="field.key" class="data-card__field">
              <label class="data-card__field-label">{{ field.label }}</label>
              <div class="data-card__field-value" :class="getFieldValueClass(field)">
                <slot :name="`field-${field.key}`" :field="field" :value="getFieldValue(field.key)">
                  {{ formatFieldValue(field, getFieldValue(field.key)) }}
                </slot>
              </div>
            </div>
          </div>

          <!-- Card Layout (default) -->
          <div v-else-if="layout === 'card'" class="data-card__card-content">
            <div v-for="field in displayFields" :key="field.key" class="data-card__field data-card__field--card">
              <label class="data-card__field-label">{{ field.label }}</label>
              <div class="data-card__field-value" :class="getFieldValueClass(field)">
                <slot :name="`field-${field.key}`" :field="field" :value="getFieldValue(field.key)">
                  {{ formatFieldValue(field, getFieldValue(field.key)) }}
                </slot>
              </div>
            </div>
          </div>

          <!-- Mail Layout (for email style) -->
          <div v-else-if="layout === 'mail'" class="data-card__mail-content">
            <div class="data-card__mail-header">
              <div class="data-card__mail-from">
                <strong>{{ getFieldValue('customerName') || getFieldValue('name') || 'Unknown' }}</strong>
              </div>
              <div class="data-card__mail-date">
                {{ formatDate(getFieldValue('createdDate') || getFieldValue('lastModified')) }}
              </div>
            </div>
          </div>

          <!-- Custom Layout -->
          <div v-else class="data-card__custom-content">
            <slot></slot>
          </div>
        </slot>
      </main>

      <!-- Footer Section -->
      <footer v-if="$slots.footer || showMetadata" class="data-card__footer">
        <slot name="footer">
          <div v-if="showMetadata" class="data-card__metadata">
            <span v-if="metadata.created" class="data-card__meta-item">
              Created: {{ formatDate(metadata.created) }}
            </span>
            <span v-if="metadata.updated" class="data-card__meta-item">
              Updated: {{ formatDate(metadata.updated) }}
            </span>
            <span v-if="metadata.author" class="data-card__meta-item">
              By: {{ metadata.author }}
            </span>
          </div>
        </slot>
      </footer>
    </template>
  </div>
</template>

<script>
export default {
  name: 'DataCard',
  props: {
    // Data
    data: {
      type: Object,
      default: () => ({})
    },
    
    // Layout Options
    layout: {
      type: String,
      default: 'card',
      validator: value => ['card', 'row', 'mail', 'custom'].includes(value)
    },
    variant: {
      type: String,
      default: 'default',
      validator: value => ['default', 'azure', 'sharepoint', 'matched', 'suggestion'].includes(value)
    },
    
    // Appearance
    compact: {
      type: Boolean,
      default: false
    },
    
    // Header
    title: {
      type: String,
      default: ''
    },
    subtitle: {
      type: String,
      default: ''
    },
    showHeader: {
      type: Boolean,
      default: true
    },
    
    // Status & Badges
    status: {
      type: Object,
      default: null
      // Example: { text: 'Active', type: 'success' }
    },
    badges: {
      type: Array,
      default: () => []
      // Example: [{ text: 'New', class: 'badge--primary' }]
    },
    
    // Fields Configuration
    fields: {
      type: Array,
      default: () => []
      // Example: [{ key: 'name', label: 'Name', type: 'text', format: null }]
    },
    maxFields: {
      type: Number,
      default: 0 // 0 means show all
    },
    
    // Actions
    clickable: {
      type: Boolean,
      default: false
    },
    selected: {
      type: Boolean,
      default: false
    },
    showActions: {
      type: Boolean,
      default: false
    },
    showEditButton: {
      type: Boolean,
      default: false
    },
    showDeleteButton: {
      type: Boolean,
      default: false
    },
    showMoreButton: {
      type: Boolean,
      default: false
    },
    
    // Button Labels
    editButtonText: {
      type: String,
      default: 'Edit'
    },
    deleteButtonText: {
      type: String,
      default: 'Delete'
    },
    moreButtonText: {
      type: String,
      default: 'More options'
    },
    
    // Metadata
    showMetadata: {
      type: Boolean,
      default: false
    },
    metadata: {
      type: Object,
      default: () => ({})
    },
    
    // Loading
    loading: {
      type: Boolean,
      default: false
    }
  },
  
  emits: ['click', 'edit', 'delete', 'more'],
  
  computed: {
    displayFields() {
      if (this.fields.length > 0) {
        return this.maxFields > 0 
          ? this.fields.slice(0, this.maxFields)
          : this.fields
      }
      
      // Auto-generate fields based on data and variant
      return this.generateFieldsFromData()
    }
  },
  
  methods: {
    handleClick() {
      if (this.clickable) {
        this.$emit('click', this.data)
      }
    },
    
    getFieldValue(key) {
      return this.data[key] || ''
    },
    
    getFieldValueClass(field) {
      return [
        `data-card__field-value--${field.type || 'text'}`,
        {
          'data-card__field-value--empty': !this.getFieldValue(field.key),
          'data-card__field-value--highlight': field.highlight
        }
      ]
    },
    
    formatFieldValue(field, value) {
      if (!value && value !== 0) return '-'
      
      switch (field.type) {
        case 'date':
          return this.formatDate(value)
        case 'currency':
          return this.formatCurrency(value)
        case 'percentage':
          return `${value}%`
        case 'boolean':
          return value ? 'Yes' : 'No'
        case 'email':
          return value
        case 'phone':
          return this.formatPhone(value)
        default:
          return field.format ? field.format(value) : value
      }
    },
    
    formatDate(date) {
      if (!date) return '-'
      try {
        return new Date(date).toLocaleDateString()
      } catch {
        return date
      }
    },
    
    formatCurrency(amount) {
      if (amount === null || amount === undefined) return '-'
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD'
      }).format(amount)
    },
    
    formatPhone(phone) {
      if (!phone) return '-'
      // Basic phone formatting - customize as needed
      return phone.replace(/(\d{3})(\d{3})(\d{4})/, '($1) $2-$3')
    },
    
    generateFieldsFromData() {
      // Generate fields based on variant and available data
      const commonFields = []
      
      switch (this.variant) {
        case 'azure':
          return this.generateAzureFields()
        case 'sharepoint':
          return this.generateSharePointFields()
        case 'matched':
          return this.generateMatchedFields()
        default:
          return this.generateDefaultFields()
      }
    },
    
    generateAzureFields() {
      const fields = []
      if (this.data.custShortDimName) fields.push({ key: 'custShortDimName', label: 'Customer Name', type: 'text' })
      if (this.data.company) fields.push({ key: 'company', label: 'Company', type: 'text' })
      if (this.data.custAppDimName) fields.push({ key: 'custAppDimName', label: 'Application', type: 'text' })
      if (this.data.prodChipNameDimName) fields.push({ key: 'prodChipNameDimName', label: 'Product', type: 'text' })
      if (this.data.createdDate) fields.push({ key: 'createdDate', label: 'Created', type: 'date' })
      return fields
    },
    
    generateSharePointFields() {
      const fields = []
      if (this.data.customerName) fields.push({ key: 'customerName', label: 'Customer', type: 'text' })
      if (this.data.contactPerson) fields.push({ key: 'contactPerson', label: 'Contact', type: 'text' })
      if (this.data.productGroup) fields.push({ key: 'productGroup', label: 'Product Group', type: 'text' })
      if (this.data.status) fields.push({ key: 'status', label: 'Status', type: 'text' })
      return fields
    },
    
    generateMatchedFields() {
      const fields = []
      if (this.data.similarity) fields.push({ key: 'similarity', label: 'Similarity', type: 'percentage', highlight: true })
      if (this.data.matchType) fields.push({ key: 'matchType', label: 'Match Type', type: 'text' })
      if (this.data.confidence) fields.push({ key: 'confidence', label: 'Confidence', type: 'text' })
      return [...this.generateAzureFields(), ...fields]
    },
    
    generateDefaultFields() {
      // Generate from first few properties of data
      return Object.keys(this.data)
        .slice(0, 6)
        .map(key => ({
          key,
          label: this.formatLabel(key),
          type: 'text'
        }))
    },
    
    formatLabel(key) {
      return key
        .replace(/([A-Z])/g, ' $1')
        .replace(/^./, str => str.toUpperCase())
    }
  }
}
</script>

<style scoped>
/* Base Card Styles */
.data-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 1rem;
  transition: all 0.2s ease;
  position: relative;
}

.data-card--clickable {
  cursor: pointer;
}

.data-card--clickable:hover {
  border-color: #3b82f6;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.data-card--selected {
  border-color: #3b82f6;
  background-color: #eff6ff;
}

.data-card--compact {
  padding: 0.75rem;
}

/* Loading State */
.data-card--loading {
  opacity: 0.6;
}

.data-card__loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 2rem;
  color: #6b7280;
}

.data-card__spinner {
  width: 20px;
  height: 20px;
  border: 2px solid #e5e7eb;
  border-top: 2px solid #3b82f6;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

/* Header */
.data-card__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.data-card__title {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
}

.data-card__subtitle {
  margin: 0.25rem 0 0 0;
  font-size: 0.875rem;
  color: #6b7280;
}

/* Actions */
.data-card__actions {
  display: flex;
  gap: 0.5rem;
}

.data-card__action-btn {
  background: none;
  border: 1px solid #d1d5db;
  border-radius: 4px;
  padding: 0.375rem;
  cursor: pointer;
  color: #6b7280;
  transition: all 0.2s;
}

.data-card__action-btn:hover {
  background-color: #f3f4f6;
  border-color: #9ca3af;
}

.data-card__action-btn--edit:hover {
  border-color: #3b82f6;
  color: #3b82f6;
}

.data-card__action-btn--delete:hover {
  border-color: #ef4444;
  color: #ef4444;
}

/* Status & Badges */
.data-card__status-section {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.data-card__status {
  padding: 0.25rem 0.75rem;
  border-radius: 1rem;
  font-size: 0.875rem;
  font-weight: 500;
}

.data-card__status--success {
  background-color: #dcfce7;
  color: #16a34a;
}

.data-card__status--warning {
  background-color: #fef3c7;
  color: #d97706;
}

.data-card__status--error {
  background-color: #fee2e2;
  color: #dc2626;
}

.data-card__badges {
  display: flex;
  gap: 0.5rem;
}

.data-card__badge {
  padding: 0.125rem 0.5rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  background-color: #f3f4f6;
  color: #374151;
}

/* Content Layouts */
.data-card__content {
  flex: 1;
}

/* Row Layout */
.data-card__row-content {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.data-card--compact .data-card__row-content {
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 0.75rem;
}

/* Card Layout */
.data-card__card-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

/* Mail Layout */
.data-card__mail-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.data-card__mail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.data-card__mail-from {
  font-weight: 600;
  color: #111827;
}

.data-card__mail-date {
  font-size: 0.875rem;
  color: #6b7280;
}

.data-card__mail-subject {
  font-weight: 500;
  color: #374151;
}

.data-card__mail-preview {
  font-size: 0.875rem;
  color: #6b7280;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

/* Fields */
.data-card__field {
  min-width: 0;
}

.data-card__field--card {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.data-card__field-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.25rem;
  display: block;
}

.data-card__field--card .data-card__field-label {
  margin-bottom: 0;
  margin-right: 1rem;
}

.data-card__field-value {
  font-size: 0.875rem;
  color: #111827;
  word-break: break-word;
}

.data-card__field-value--empty {
  color: #9ca3af;
  font-style: italic;
}

.data-card__field-value--highlight {
  font-weight: 600;
  color: #3b82f6;
}

.data-card__field-value--percentage {
  font-weight: 500;
}

.data-card__field-value--email {
  color: #3b82f6;
}

/* Footer */
.data-card__footer {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #f3f4f6;
}

.data-card__metadata {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}

.data-card__meta-item {
  font-size: 0.75rem;
  color: #6b7280;
}

/* Variants */
.data-card--azure {
  border-left: 4px solid #0078d4;
}

.data-card--sharepoint {
  border-left: 4px solid #8661c5;
}

.data-card--matched {
  border-left: 4px solid #16a34a;
}

.data-card--suggestion {
  border-left: 4px solid #f59e0b;
}

/* Animations */
@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Responsive */
@media (max-width: 768px) {
  .data-card__row-content {
    grid-template-columns: 1fr;
  }
  
  .data-card__header {
    flex-direction: column;
    gap: 0.5rem;
    align-items: flex-start;
  }
  
  .data-card__metadata {
    flex-direction: column;
    gap: 0.25rem;
  }
}
</style>