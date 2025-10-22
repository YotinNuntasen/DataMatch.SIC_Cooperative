/**
 * Normalize date to ISO string
 */
export function normalizeDateToISOString(value) {
  if (value === undefined || value === null) {
    return null;
  }

  let dateObj;
  if (value instanceof Date) {
    dateObj = value;
  } else {
    const s = String(value).trim();
    if (!s) return null;
    dateObj = new Date(s);
  }

  if (isNaN(dateObj.getTime())) {
    console.warn("Invalid date value, converting to null:", value);
    return null;
  }

  return dateObj.toISOString();
}

/**
 * Calculate revenue from Azure item
 */
export function calculateRevenue(azureItem) {
  const rawTotalSales = azureItem.calculatedRevenue || 
                       azureItem.totalSales || 
                       azureItem.TotalSales || 
                       "0";
  const cleanedTotalSales = parseFloat(
    String(rawTotalSales).replace(/[^0-9.-]+/g, "")
  );
  return cleanedTotalSales || 0;
}

/**
 * Format currency display
 */
export function formatCurrency(amount, currency = 'USD') {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency,
    minimumFractionDigits: 2
  }).format(amount || 0);
}

/**
 * Format date display
 */
export function formatDate(date, format = 'short') {
  if (!date) return 'N/A';
  
  const dateObj = new Date(date);
  if (isNaN(dateObj.getTime())) return 'Invalid Date';
  
  const options = {
    short: { year: 'numeric', month: 'short', day: 'numeric' },
    long: { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' },
    time: { 
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    }
  };
  
  return new Intl.DateTimeFormat('en-US', options[format] || options.short)
    .format(dateObj);
}

/**
 * Generic sorter function
 */
export function sortData(data, sortKey, sortDirection) {
  if (!sortKey || !Array.isArray(data)) return data;

  return [...data].sort((a, b) => {
    let valA = a[sortKey];
    let valB = b[sortKey];

    // Handle dates
    if (sortKey.includes('Date')) {
      valA = valA ? new Date(valA).getTime() : 0;
      valB = valB ? new Date(valB).getTime() : 0;
    } 
    // Handle numbers (revenue, similarity)
    else if (typeof valA === 'number' || sortKey.includes('Revenue') || sortKey === 'similarity') {
      valA = parseFloat(valA || 0);
      valB = parseFloat(valB || 0);
    } 
    // Handle strings
    else {
      valA = String(valA || "").toLowerCase();
      valB = String(valB || "").toLowerCase();
    }

    if (valA < valB) return sortDirection === "asc" ? -1 : 1;
    if (valA > valB) return sortDirection === "asc" ? 1 : -1;
    return 0;
  });
}

/**
 * Create merged person document
 */
export function createMergedPersonDocument(sharePointItem, azureItem) {
  const itemRevenue = calculateRevenue(azureItem);
  const nowISOString = new Date().toISOString();
  
  return {
    PartitionKey: azureItem.PartitionKey || "MergedCustomer",
    RowKey: azureItem.RowKey || azureItem.rowKey,
    custShortDimName: azureItem.custShortDimName || sharePointItem.customerName || "",
    postingDate: normalizeDateToISOString(azureItem.postingDate),
    prefixdocumentNo: azureItem.prefixdocumentNo || "",
    selltoCustName_SalesHeader: azureItem.selltoCustName_SalesHeader || "",
    systemRowVersion: azureItem.systemRowVersion || "",
    documentDate: normalizeDateToISOString(azureItem.documentDate || sharePointItem.s9DWINEntryDate),
    documentNo: azureItem.documentNo || "",
    itemReferenceNo: azureItem.PCode || sharePointItem.productCode || "",
    lineNo: azureItem.lineNo || 0,
    no: azureItem.no || "",
    quantity: azureItem.quantity || 0,
    sellToCustomerNo: azureItem.sellToCustomerNo || "",
    shipmentNo: azureItem.shipmentNo || "",
    sodocumentNo: azureItem.sodocumentNo || "",
    srodocumentNo: azureItem.SrodocumentNo || "",
    description: azureItem.description || sharePointItem.productInterest || "",
    unitOfMeasure: azureItem.UnitOfMeasure || "",
    unitPrice: azureItem.unitPrice || 0,
    lineDiscount: azureItem.LineDiscount || 0,
    lineAmount: azureItem.lineAmount || 0,
    currencyCode: azureItem.currencyCode || "",
    currencyRate: azureItem.currencyRate || 0,
    salesPerUnit: azureItem.SalesPerUnit || 0,
    totalSales: azureItem.totalSales || azureItem.TotalSales || 0,
    podocumentNo: azureItem.PodocumentNo || "",
    custAppDimName: azureItem.custAppDimName || "",
    prodChipNameDimName: azureItem.prodChipNameDimName || sharePointItem.productGroup || "",
    regionDimName3: azureItem.regionDimName3 || "",
    salespersonDimName: azureItem.SalesName || sharePointItem.customerNameSalePersonCode || "",
    "opportunity ID": sharePointItem.opportunityId || "",
    calculatedRevenue: itemRevenue,
    revenueCalculationTimestamp: nowISOString,
    modified: nowISOString,
    matchedDate: nowISOString,
    id: azureItem.RowKey || azureItem.id,
  };
}

/**
 * Get unique record key
 */
export function getRecordKey(record) {
  return record.RowKey || record.rowKey || record.id || 
         `${record.customerName || 'unknown'}_${Date.now()}`;
}

/**
 * Debounce function for search
 */
export function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

/**
 * Deep clone object
 */
export function deepClone(obj) {
  if (obj === null || typeof obj !== 'object') return obj;
  if (obj instanceof Date) return new Date(obj.getTime());
  if (obj instanceof Array) return obj.map(item => deepClone(item));
  
  const cloned = {};
  for (let key in obj) {
    if (obj.hasOwnProperty(key)) {
      cloned[key] = deepClone(obj[key]);
    }
  }
  return cloned;
}

/**
 * Validate RowKey
 */
export function validateRowKey(rowKey) {
  return rowKey && rowKey !== "-" && rowKey.trim() !== "";
}