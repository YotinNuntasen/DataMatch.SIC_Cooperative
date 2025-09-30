<template>
  <div class="azure-data-row-card">
    <!-- Customer Name with comparison color -->
    <div class="cell actions-cell">
      <button @click.stop="emitMatch" class="match-btn">Match</button>
      <button @click.stop="emitCompare" class="compare-btn">View</button>
    </div>

    <div class="cell customer-name-cell" :class="getComparisonClass('customerName')">
      {{ data.customerName || data.selltoCustName_SalesHeader || 'N/A' }}
      <span v-if="showComparisonIcon('customerName')" class="comparison-icon">
        {{ getComparisonIcon('customerName') }}
      </span>
    </div>

    <!-- Short Name - no comparison -->
    <div class="cell">{{ data.custShortDimName || 'N/A' }}</div>

    <!-- Product with comparison color -->
    <div class="cell" :class="getComparisonClass('product')">
      {{ data.custAppDimName || 'N/A' }}
      <span v-if="showComparisonIcon('product')" class="comparison-icon">
        {{ getComparisonIcon('product') }}
      </span>
    </div>

    <!-- Product Code with comparison color -->
    <div class="cell" :class="getComparisonClass('productCode')">
      {{ data.pCode || data.itemReferenceNo || 'N/A' }}
      <span v-if="showComparisonIcon('productCode')" class="comparison-icon">
        {{ getComparisonIcon('productCode') }}
      </span>
    </div>

    <!-- Document Date with comparison color -->
    <div class="cell" :class="getComparisonClass('documentDate')">
      {{ formatDate(data.documentDate) }}
      <span v-if="showComparisonIcon('documentDate')" class="comparison-icon">
        {{ getComparisonIcon('documentDate') }}
      </span>
    </div>

    <!-- Salesperson with comparison color -->
    <div class="cell" :class="getComparisonClass('salesperson')">
      {{ data.salespersonDimName || 'N/A' }}
      <span v-if="showComparisonIcon('salesperson')" class="comparison-icon">
        {{ getComparisonIcon('salesperson') }}
      </span>
    </div>

    <!-- Region with comparison color -->
    <div class="cell" :class="getComparisonClass('region')">
      {{ data.regionDimName3 || 'N/A' }}
      <span v-if="showComparisonIcon('region')" class="comparison-icon">
        {{ getComparisonIcon('region') }}
      </span>
    </div>

    <!-- Chip Name - no comparison -->
    <div class="cell">{{ data.prodChipNameDimName || 'N/A' }}</div>


    <!-- Other fields without comparison -->
    <div class="cell">{{ data.documentNo || 'N/A' }}</div>
    <div class="cell">{{ data.lineNo || 'N/A' }}</div>
    <div class="cell">{{ data.no || 'N/A' }}</div>
    <div class="cell">{{ data.quantity || 'N/A' }}</div>
    <div class="cell">{{ data.sellToCustomerNo || 'N/A' }}</div>
    <div class="cell">{{ data.unitPrice || 'N/A' }}</div>
    <div class="cell">{{ data.lineAmount || 'N/A' }}</div>
    <div class="cell">{{ data.currencyRate || 'N/A' }}</div>
    <div class="cell">{{ data.salesPerUnit || 'N/A' }}</div>
    <div class="cell">{{ data.totalSales || 'N/A' }}</div>
    <div class="cell description-cell">{{ data.description || 'N/A' }}</div>

    <!-- Actions -->

  </div>
</template>


<script>
import { getFieldComparisons } from '../utils/similarity';
import { formatDate } from '@/utils/formatters';

export default {
  name: 'AzureDataRowCard',
  props: {
    data: {
      type: Object,
      required: true
    },
    sharepointItem: {
      type: Object,
      required: true
    },
    similarity: {
      type: Number,
      required: true
    }
  },
  emits: ['match', 'compare'],
  computed: {
    fieldComparisons() {
      return getFieldComparisons(this.sharepointItem, this.data);
    }
  },
  methods: {
    // ✅ Expose formatDate from utils
    formatDate,

    getComparisonClass(fieldType) {
      const status = this.fieldComparisons[fieldType];
      return `comparison-${status}`;
    },

    showComparisonIcon(fieldType) {
      const status = this.fieldComparisons[fieldType];
      return status && status !== 'neutral';
    },

    getComparisonIcon(fieldType) {
      const status = this.fieldComparisons[fieldType];
      switch (status) {
        case 'exact-match': return '✓';
        case 'high-match': return '✓';
        case 'partial-match': return '≈';
        case 'medium-match': return '~';
        case 'low-match': return '?';
        case 'no-match': return '✗';
        case 'missing': return '⚠';
        case 'invalid': return '!';
        default: return '';
      }
    },

    emitMatch() {
      const rowKey = this.data.RowKey || this.data.rowKey;
      console.log('🎯 Matching item with RowKey:', rowKey);

      this.$emit('match', {
        sharePointItem: this.sharepointItem,
        azureItem: {
          ...this.data,
          RowKey: rowKey
        },
        similarity: this.similarity
      });
    },
    
    emitCompare() {
      this.$emit('compare', {
        azureItem: this.data,
        sharepointItem: this.sharepointItem,
        similarity: this.similarity
      });
    }
  }
}
</script>

<style scoped>
.azure-data-row-card {
  display: grid;
  grid-template-columns:

    100px 200px 120px 150px 140px 150px 120px 120px 140px 120px 80px 100px 100px 120px 100px 120px 130px 130px 110px 200px;



  align-items: stretch;
  background-color: white;
  border-bottom: 1px solid #f1f1f1;
  transition: background-color 0.2s ease;
  grid-gap: 0;
}

.azure-data-row-card:hover {
  background-color: #f8f9fa;
}

.cell {
  padding: 6px 6px;
  font-size: 0.95rem;
  font-weight: 500;
  color: #000000;
  word-break: break-word;
  overflow-wrap: break-word;
  -webkit-hyphens: auto;
  -ms-hyphens: auto;
  hyphens: auto;
  border-right: 1px solid #eee;
  display: flex;
  align-items: flex-start;
  min-height: 48px;
  line-height: 1.4;
  position: relative;
  transition: all 0.3s ease;
}

.cell:last-child {
  border-right: none;
}

/* === ปรับปรุง Comparison Color Classes === */

/* เขียว - สำหรับข้อมูลที่ Match */
.comparison-exact-match {
  background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%) !important;
  border-left: 4px solid #28a745 !important;
  color: #155724 !important;
  font-weight: 600;
}

.comparison-high-match {
  background: linear-gradient(135deg, #d4edda 0%, #c8f7c5 100%) !important;
  border-left: 4px solid #20c997 !important;
  color: #0c5460 !important;
  font-weight: 500;
}

/* เหลือง - สำหรับข้อมูลที่ใกล้เคียง */
.comparison-partial-match {
  background: linear-gradient(135deg, #fff3cd 0%, #ffeaa7 100%) !important;
  border-left: 4px solid #ffc107 !important;
  color: #856404 !important;
}

.comparison-medium-match {
  background: linear-gradient(135deg, #fef2e6 0%, #fed7aa 100%) !important;
  border-left: 4px solid #fd7e14 !important;
  color: #8a4a00 !important;
}

/* แดง - สำหรับข้อมูลที่ไม่ Match */
.comparison-no-match {
  background: linear-gradient(135deg, #f8d7da 0%, #f1b0b7 100%) !important;
  border-left: 4px solid #dc3545 !important;
  color: #721c24 !important;
  font-weight: 500;
}

.comparison-low-match {
  background: linear-gradient(135deg, #ffe6e6 0%, #ffcccc 100%) !important;
  border-left: 4px solid #ff6b6b !important;
  color: #721c24 !important;
}

/* เทา - สำหรับข้อมูลที่หายไป */
.comparison-missing {
  background: linear-gradient(135deg, #e2e3e5 0%, #d6d8db 100%) !important;
  border-left: 4px solid #6c757d !important;
  color: #495057 !important;
  font-style: italic;
}

.customer-name-cell {
  font-weight: 500;
  color: #0056b3;
}

.customer-name-cell.comparison-exact-match,
.customer-name-cell.comparison-high-match {
  color: #155724 !important;
}

.customer-name-cell.comparison-no-match {
  color: #721c24 !important;
}

.description-cell {
  font-style: italic;
  color: #666;
}

/* Actions Cell */
.actions-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
  align-items: center;
  justify-content: center;
  padding: 4px;
  background-color: white !important;
  border-left: none !important;
}

.match-btn,
.compare-btn {
  padding: 4px 8px;
  font-size: 0.7rem;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s ease;
  width: 100%;
  white-space: nowrap;
}

.match-btn {
  background-color: #28a745;
  color: white;
  border: 1px solid #28a745;
}

.match-btn:hover {
  background-color: #218838;
}

.compare-btn {
  background-color: #007bff;
  color: white;
  border: 1px solid #007bff;
}

.compare-btn:hover {
  background-color: #0056b3;
}

/* เพิ่ม Hover Effects ที่สวยขึ้น */
.cell.comparison-exact-match:hover {
  background: linear-gradient(135deg, #c3e6cb 0%, #b1dfbb 100%) !important;
  transform: translateX(2px);
}

.cell.comparison-high-match:hover {
  background: linear-gradient(135deg, #c8f7c5 0%, #b8f2b5 100%) !important;
  transform: translateX(2px);
}

.cell.comparison-partial-match:hover {
  background: linear-gradient(135deg, #ffeaa7 0%, #fdd835 100%) !important;
  transform: translateX(2px);
}

.cell.comparison-medium-match:hover {
  background: linear-gradient(135deg, #fed7aa 0%, #fcc419 100%) !important;
  transform: translateX(2px);
}

.cell.comparison-no-match:hover {
  background: linear-gradient(135deg, #f1b0b7 0%, #ea868f 100%) !important;
  transform: translateX(2px);
}

.cell.comparison-low-match:hover {
  background: linear-gradient(135deg, #ffcccc 0%, #ff9999 100%) !important;
  transform: translateX(2px);
}

.cell.comparison-missing:hover {
  background: linear-gradient(135deg, #d6d8db 0%, #ced4da 100%) !important;
  transform: translateX(2px);
}

.comparison-icon {
  margin-left: 4px;
  font-size: 0.8rem;
  opacity: 0.8;
}
</style>