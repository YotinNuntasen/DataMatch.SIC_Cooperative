// utils/apiClient.js - ปรับให้ใช้งานง่าย
import axios from "axios";
import { apiConfig, defaultHeaders } from "../config/apiConfig";

export function createApiClient(config = {}) {
  const client = axios.create({
    baseURL: config.baseURL || apiConfig.sharepoint.baseURL,
    timeout: config.timeout || 30000,
    headers: { ...defaultHeaders, ...config.headers }
  });

  // Request interceptor
  client.interceptors.request.use(
    (requestConfig) => {
      if (apiConfig.development.enableDetailedLogging) {
        console.log(`📤 ${requestConfig.method?.toUpperCase()} ${requestConfig.url}`);
      }
      return requestConfig;
    },
    (error) => Promise.reject(error)
  );

  // Response interceptor
  client.interceptors.response.use(
    (response) => {
      if (apiConfig.development.enableDetailedLogging) {
        console.log(`✅ ${response.status} ${response.config.url}`);
      }
      return response;
    },
    (error) => {
      console.error(`❌ API Error:`, {
        status: error.response?.status,
        url: error.config?.url,
        message: error.response?.data?.message || error.message
      });
      return Promise.reject(error);
    }
  );

  return client;
}

// Export singleton instance
export const apiClient = createApiClient();
export default apiClient;