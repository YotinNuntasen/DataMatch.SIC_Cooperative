<template>
    <div class="table-container">
        <!-- ปรับปรุง Color Legend ใหม่ -->
        <div class="comparison-legend">
            <!-- ไม่ต้องมี h4 เพื่อลดความสูง -->
            <div class="legend-items">
                <div class="legend-item">
                    <div class="legend-circle exact-match"></div>
                    <span class="legend-text">Exact Match</span>
                </div>
                <div class="legend-item">
                    <div class="legend-circle partial-match"></div>
                    <span class="legend-text">Partial Match</span>
                </div>
                <div class="legend-item">
                    <div class="legend-circle no-match"></div>
                    <span class="legend-text">No Match</span>
                </div>
            </div>
        </div>

        <div class="table-scroll-wrapper">
            <div class="data-table">
                <!-- HEADER เดิม -->
                <div class="table-header">
                    <div class="header-cell actions-header">Actions</div>
                    <div class="header-cell">Customer Name*</div>
                    <div class="header-cell">ShortName</div>
                    <div class="header-cell">Product Group*</div>
                    <div class="header-cell">PCode*</div>
                     <div class="header-cell">Document Date*</div>
                    <div class="header-cell">SalesName*</div>
                    <div class="header-cell">Region*</div>
                    <div class="header-cell">Chip Name</div>
                    <div class="header-cell">Document No</div>
                    <div class="header-cell">Line No</div>
                    <div class="header-cell">No</div>
                    <div class="header-cell">Quantity</div>
                    <div class="header-cell">Customer No</div>
                    <div class="header-cell">Unit Price</div>
                    <div class="header-cell">Line Amount</div>
                    <div class="header-cell">Currency Rate</div>
                    <div class="header-cell">Sales Per Unit</div>
                    <div class="header-cell">Total Sales</div>
                    <div class="header-cell description-header">Description</div>

                </div>

                <!-- BODY เดิม -->
                <div class="table-body-scroll-container">
                    <template v-if="similarRecords && similarRecords.length > 0">
                        <AzureDataRowCard v-for="record in similarRecords" :key="record.id || record.rowKey"
                            :data="record" :sharepoint-item="selectedSharePointItem" :similarity="record.similarity"
                            @match="handleRowMatch" @compare="openCompareModal" />
                    </template>
                    <div v-else class="empty-row-message">
                        No similar records to display.
                    </div>
                </div>
            </div>
        </div>

        <!-- Modal เดิม (ไม่มีการแก้ไข) -->
        <div v-if="showCompareModal" class="modal-overlay" @click="closeModal">
            <!-- Modal content เดิม -->
        </div>
    </div>
</template>

<script>
// ส่วน <script> ไม่มีการแก้ไข สามารถคงไว้เหมือนเดิมได้
import AzureDataRowCard from './AzureDataRowCard.vue';
import { formatDate, formatCurrency } from '@/utils/formatters';
import {
    calculateNameSimilarity,
    calculatePcodeSimilarity,
    calculateDateSimilarity,
    getStatusFromScore
} from '../utils/similarity';

export default {
    name: 'AzureFullTable',
    components: {
        AzureDataRowCard
    },
    props: {
        similarRecords: {
            type: Array,
            required: true,
            default: () => []
        },
        selectedSharePointItem: {
            type: Object,
            required: true
        }
    },
    emits: ['match'],
    data() {
        return {
            showCompareModal: false,
            currentAzureItem: null,
            currentSharepointItem: null,
            currentSimilarity: 0
        }
    },
    computed: {
        modalScoreClass() {
            if (this.currentSimilarity >= 90) return 'excellent'
            if (this.currentSimilarity >= 80) return 'good'
            if (this.currentSimilarity >= 60) return 'medium'
            return 'low'
        },
        comparisonFieldsInModal() {
            if (!this.currentSharepointItem || !this.currentAzureItem) return [];

            const sp = this.currentSharepointItem;
            const az = this.currentAzureItem;

            const fieldsToCompare = [];

            const customerNameScore = calculateNameSimilarity(
                sp.customerName || '', az.customerName || az.name || ''
            );
            fieldsToCompare.push({
                label: 'Customer Name',
                sharePointValue: sp.customerName || 'N/A',
                azureValue: az.customerName || az.name || 'N/A',
                score: Math.round(customerNameScore),
                status: getStatusFromScore(customerNameScore)
            });

            const productCodeScore = calculatePcodeSimilarity(
                sp.productCode || '', az.itemReferenceNo || ''
            );
            fieldsToCompare.push({
                label: 'Product Code (PCode)',
                sharePointValue: sp.productCode || 'N/A',
                azureValue: az.itemReferenceNo || 'N/A',
                score: Math.round(productCodeScore),
                status: getStatusFromScore(productCodeScore)
            });

            const documentDateScore = calculateDateSimilarity(
                sp.s9DWINEntryDate || '', az.documentDate || ''
            );
            fieldsToCompare.push({
                label: 'D-Win / Document Date',
                sharePointValue: this.formatDate(sp.s9DWINEntryDate) || 'N/A',
                azureValue: this.formatDate(az.documentDate) || 'N/A',
                score: Math.round(documentDateScore),
                status: getStatusFromScore(documentDateScore)
            });

            // Other fields that might be useful for comparison but not directly scored
            fieldsToCompare.push({
                label: 'Salesperson',
                sharePointValue: sp.customerNameSalePersonCode || 'N/A',
                azureValue: az.salespersonDimName || 'N/A',
                score: 'N/A',
                status: this.compareTextDirectly(sp.customerNameSalePersonCode, az.salespersonDimName)
            });

            fieldsToCompare.push({
                label: 'Region',
                sharePointValue: sp.country || 'N/A',
                azureValue: az.regionDimName3 || 'N/A',
                score: 'N/A',
                status: this.compareTextDirectly(sp.country, az.regionDimName3)
            });

            fieldsToCompare.push({
                label: 'Uniquerow',
                sharePointValue: 'N/A (Azure Only)',
                azureValue: az.uniquerow || 'N/A',
                score: 'N/A',
                status: 'neutral'
            });

            fieldsToCompare.push({
                label: 'Document',
                sharePointValue: 'N/A (Azure Only)',
                azureValue: az.document || 'N/A',
                score: 'N/A',
                status: 'neutral'
            });

            fieldsToCompare.push({
                label: 'Line No.',
                sharePointValue: 'N/A (Azure Only)',
                azureValue: az.lineNo || 'N/A',
                score: 'N/A',
                status: 'neutral'
            });

            return fieldsToCompare;
        }
    },
    methods: {
        handleRowMatch({ sharePointItem, azureItem, similarity }) {
            this.$emit('match', { sharePointItem, azureItem, similarity });
        },

        openCompareModal({ azureItem, sharepointItem, similarity }) {
            this.currentAzureItem = azureItem;
            this.currentSharepointItem = sharepointItem;
            this.currentSimilarity = similarity;
            this.showCompareModal = true;
            document.body.style.overflow = 'hidden';
        },

        closeModal() {
            this.showCompareModal = false;
            this.currentAzureItem = null;
            this.currentSharepointItem = null;
            this.currentSimilarity = 0;
            document.body.style.overflow = 'auto';
        },

        confirmMatch() {
            if (this.currentAzureItem && this.currentSharepointItem) {
                this.handleRowMatch({
                    sharePointItem: this.currentSharepointItem,
                    azureItem: this.currentAzureItem,
                    similarity: this.currentSimilarity
                });
            }
            this.closeModal();
        },

        compareTextDirectly(val1, val2) {
            const v1 = String(val1 || '').toLowerCase().trim();
            const v2 = String(val2 || '').toLowerCase().trim();
            if (v1 === '' && v2 === '') return 'neutral';
            if (v1 === v2 && v1 !== '') return 'exact-match';
            if (v1.includes(v2) || v2.includes(v1)) return 'partial-match';
            return 'no-match';
        }
    },

    beforeUnmount() {
        if (this.showCompareModal) {
            document.body.style.overflow = 'auto';
        }
    }
}
</script>

<style scoped>
.table-container {
    height: 100%;
    /* เพิ่มบรรทัดนี้ */
    margin-top: 16px;
    border-radius: 8px;
    background-color: white;
    display: flex;
    /* เพิ่มบรรทัดนี้ */
    flex-direction: column;
    /* เพิ่มบรรทัดนี้ */
}

.comparison-legend {
    background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
    border: 1px solid #dee2e6;
    border-radius: 8px;
    /* ลดความโค้งมนลงเล็กน้อย */
    padding: 8px 16px;
    /* ลด padding บน-ล่าง */
    margin-bottom: 12px;
    /* ลดระยะห่างด้านล่าง */
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.04);
    flex-shrink: 0;
}

.legend-items {
    display: flex;
    /* เปลี่ยนเป็น flex เพื่อให้อยู่บรรทัดเดียวกัน */
    flex-wrap: wrap;
    /* ทำให้ขึ้นบรรทัดใหม่ได้ถ้าไม่พอ */
    gap: 20px;
    /* เพิ่มระยะห่างระหว่าง items */
    justify-content: center;
    /* จัดให้อยู่กึ่งกลาง */
    align-items: center;
}

/* สิ้นสุดการแก้ไข Style */

.legend-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 4px 8px;
    background: white;
    border-radius: 6px;
    border: 1px solid transparent;
}

.legend-circle {
    width: 14px;
    height: 14px;
    border-radius: 50%;
    flex-shrink: 0;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.15);
}

.legend-text {
    font-size: 0.85rem;
    font-weight: 500;
    color: #495057;
}

.legend-circle.exact-match {
    background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
}

.legend-circle.partial-match {
    background: linear-gradient(135deg, #ffc107 0%, #ffed4e 100%);
}

.legend-circle.no-match {
    background: linear-gradient(135deg, #dc3545 0%, #e74c3c 100%);
}

.table-scroll-wrapper {
    overflow-x: auto;
    border: 1px solid #dee2e6;
    border-radius: 8px;
    flex: 1;
    min-height: 0;
}

.data-table {
    display: flex;
    flex-direction: column;
    min-width: 2550px;
    height: 100%;
}

.table-header {
    display: grid;
    grid-template-columns:
        100px
        200px
        120px
        150px
        140px
        150px
        120px
        120px
        140px
        120px
        80px
        100px
        100px
        120px
        100px
        120px
        130px
        130px
        110px
        200px;

    background-color: #f8f9fa;
    font-weight: 600;
    font-size: 0.95rem;
    color: #495057;
    border-bottom: 2px solid #dee2e6;
    position: sticky;
    top: 0;
    z-index: 10;
    grid-gap: 0;
}

.header-cell {
    padding: 8px 8px;
    display: flex;
    align-items: center;
    word-break: break-word;
    overflow: hidden;
    text-overflow: ellipsis;
    background-color: #f8f9fa;
    border-right: 1px solid #dee2e6;
    justify-content: flex-start;
    line-height: normal;
    height: auto;
}

.description-header {
    align-items: flex-start;
}

.header-cell:last-child {
    border-right: none;
}

.actions-header {
    justify-content: center;
}

.table-body-scroll-container {
    flex: 1;
    overflow-y: auto;
    overflow-x: hidden;
    min-height: 0;
}

.empty-row-message {
    padding: 20px;
    text-align: center;
    color: #868e96;
    background-color: #fcfcfc;
    border-top: 1px solid #dee2e6;
    font-size: 0.9rem;
    grid-column: 1 / -1;
}

.table-scroll-wrapper::-webkit-scrollbar {
    width: 10px;
    height: 10px;
}

.table-scroll-wrapper::-webkit-scrollbar-track {
    background: #f1f1f1;
}

.table-scroll-wrapper::-webkit-scrollbar-thumb {
    background: #ced4da;
    border-radius: 5px;
}

.table-body-scroll-container::-webkit-scrollbar {
    width: 0px;
}

.table-body-scroll-container::-webkit-scrollbar-track {
    background: transparent;
}

.table-body-scroll-container::-webkit-scrollbar-thumb {
    background: transparent;
}

.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.7);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
    backdrop-filter: blur(4px);
}

@keyframes modalSlideIn {
    from {
        transform: scale(0.9) translateY(-20px);
        opacity: 0;
    }

    to {
        transform: scale(1) translateY(0);
        opacity: 1;
    }
}
</style>