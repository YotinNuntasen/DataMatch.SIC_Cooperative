import axios from "axios";
import store from "../store";

class SharePointService {
  constructor() {
    this.baseURL = `https://nbo-matching-fmgddgbhfkgjddhj.southeastasia-01.azurewebsites.net/api/`;
    this.apiClient = axios.create({
      baseURL: this.baseURL,
      timeout: 30000,
      headers: {
        "Content-Type": "application/json",
      },
    });

    this.apiClient.interceptors.request.use(
      (config) => {
        console.log(
          `🔄 API Request: ${config.method?.toUpperCase()} ${config.url}`
        );
        return config;
      },
      (error) => Promise.reject(error)
    );

    this.apiClient.interceptors.response.use(
      (response) => {
        console.log(
          `✅ API Response: ${response.status} ${response.config.url}`
        );
        return response;
      },
      (error) => {
        console.error(
          `❌ API Error: ${error.response?.status} for ${error.config?.url}`,
          error.response?.data || error.message
        );
        return Promise.reject(error);
      }
    );
  }

  async getAuthHeaders() {
    const token = await store.dispatch("auth/acquireSharePointToken");
    return {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    };
  }

  async getSharePointData() {
  try {
    console.log("📊 Fetching SharePoint data from backend...");
    const headers = await this.getAuthHeaders();
    const response = await this.apiClient.get("/sharepoint/contacts", {
      headers,
    });

   
    const outerApiResponse = response.data;
    
    const innerApiResponse = outerApiResponse?.data; 

    if (innerApiResponse && innerApiResponse.success && Array.isArray(innerApiResponse.data)) {
    
      const processedData = this.processSharePointData(innerApiResponse.data);

      console.log(
        `✅ API Message: "${innerApiResponse.message}". Processed ${processedData.length} records.`
      );
      
      return processedData;

    } else {
    
      const errorMessage = innerApiResponse?.message || outerApiResponse?.message || "Invalid or nested response structure from backend.";
      throw new Error(errorMessage);
    }

  } catch (error) {
    console.error("❌ Failed to fetch SharePoint data:", error);

    if (process.env.NODE_ENV === "development") {
      console.warn(
        "⚠️ Could not fetch real data. Falling back to MOCK data for development."
      );
      return this.getMockSharePointData();
    }
    
    
    throw new Error(
      error.response?.data?.data?.message || 
      error.response?.data?.message ||        
      error.message ||                        
      "Could not retrieve SharePoint data. Please contact support."
    );
  }
}

  processSharePointData(data) {
    if (!Array.isArray(data)) {
      console.error("Data received for processing is not an array:", data);
      return [];
    }
    console.log("Processing raw data from backend:", data);

    return data;
  }

  async getDiagnosticInfo() {
    try {
      const headers = await this.getAuthHeaders();
      const response = await this.apiClient.get("/sharepoint/diagnose-user", {
        headers,
      });
      return response.data;
    } catch (error) {
      console.error("❌ SharePoint diagnostic failed:", error);
      throw error;
    }
  }

  async searchSharePointData(query) {
    try {
      const headers = await this.getAuthHeaders();
      const response = await this.apiClient.get("/sharepoint/search", {
        headers,
        params: { query },
      });

      if (response.data && Array.isArray(response.data.data)) {
        return this.processSharePointData(response.data.data);
      }
      return [];
    } catch (error) {
      console.error("❌ Failed to search SharePoint data:", error);
      throw error;
    }
  }

  async getSharePointLists() {
    try {
      const headers = await this.getAuthHeaders();
      const response = await this.apiClient.get("/sharepoint/lists", {
        headers,
      });
      return response.data;
    } catch (error) {
      console.error("❌ Failed to fetch SharePoint lists:", error);
      if (process.env.NODE_ENV === "development") {
        console.warn("⚠️ Falling back to MOCK SharePoint lists.");
        return this.getMockSharePointLists();
      }
      throw error;
    }
  }

  async testConnection() {
    try {
      const headers = await this.getAuthHeaders();
      const response = await this.apiClient.get("/sharepoint/test-connection", {
        headers,
      });
      return response.data;
    } catch (error) {
      console.error("❌ SharePoint connection test failed:", error);
      throw error;
    }
  }

  async validateUserPermissions() {
    try {
      const headers = await this.getAuthHeaders();
      const response = await this.apiClient.get(
        "/sharepoint/validate-permissions",
        { headers }
      );
      return response.data;
    } catch (error) {
      console.error("❌ Permission validation failed:", error);
      throw error;
    }
  }

}

export default new SharePointService();