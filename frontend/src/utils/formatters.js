
/**
 * Centralized formatting utilities
 */

export const formatDate = (dateString, locale = 'th-TH', options = {}) => {
  if (!dateString) return 'N/A';
  
  try {
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return dateString;
    
    const defaultOptions = {
      day: '2-digit',
      month: 'short', 
      year: 'numeric',
      ...options
    };
    
    return date.toLocaleDateString(locale, defaultOptions);
  } catch (e) {
    console.warn('Date formatting error:', e);
    return dateString;
  }
};

export const formatCurrency = (value, currency = 'THB', locale = 'th-TH') => {
  if (value === null || value === undefined || isNaN(value)) return 'N/A';
  if (value === 0) return '0';
  
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value);
};

export const formatPercentage = (value, decimals = 0) => {
  if (value === null || value === undefined || isNaN(value)) return 'N/A';
  return `${value.toFixed(decimals)}%`;
};

export const formatPhone = (phone) => {
  if (!phone) return 'N/A';
  return phone.replace(/(\d{3})(\d{3})(\d{4})/, '($1) $2-$3');
};

// Export all as default
export default {
  formatDate,
  formatCurrency,
  formatPercentage,
  formatPhone
};