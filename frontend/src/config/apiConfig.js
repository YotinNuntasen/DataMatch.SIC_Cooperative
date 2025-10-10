// config/apiConfig.js

const isDevelopment = process.env.NODE_ENV === 'development';

export const apiConfig = {
  sharepoint: {
    baseURL: process.env.VUE_APP_API_BASE_URL || 'https://nbo-matching.azurewebsites.net/api',
    timeout: parseInt(process.env.VUE_APP_API_TIMEOUT) || 30000,
    endpoints: {
      contacts: "/sharepoint/contacts",
      diagnose: "/sharepoint/diagnose-user",
      search: "/sharepoint/search",
      lists: "/sharepoint/lists",
      testConnection: "/sharepoint/test-connection",
      validatePermissions: "/sharepoint/validate-permissions",
    }
  },
  azure: {
    baseURL: process.env.VUE_APP_AZURE_API_BASE_URL || 'https://nbo-matching.azurewebsites.net/api',
    timeout: parseInt(process.env.VUE_APP_AZURE_API_TIMEOUT) || 30000,
  },
  development: {
    enableMockData: isDevelopment,
    enableDetailedLogging: isDevelopment,
    enableFallbacks: isDevelopment,
  }
};

export const defaultHeaders = {
  "Content-Type": "application/json",
  "Accept": "application/json",
};