import axios from "axios";
// 1. นำเข้าฟังก์ชัน transformPersonnelInfo จากไฟล์ที่คุณสร้าง
import { transformPersonnelInfo } from "../services/personnelMapping.js";

class AzureService {
  constructor() {
    this.baseURL = "http://localhost:7204/api";
    this.apiClient = axios.create({
      baseURL: this.baseURL,
      timeout: 30000,
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
      },
    });

    // Request interceptor to add auth token
    this.apiClient.interceptors.request.use(
      (config) => {
        const token = this.getStoredToken();
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        console.log(
          `🔄 Azure API Request: ${config.method?.toUpperCase()} ${config.url}`
        );
        return config;
      },
      (error) => {
        console.error("❌ Request interceptor error:", error);
        return Promise.reject(error);
      }
    );

    // Response interceptor for error handling
    this.apiClient.interceptors.response.use(
      (response) => {
        console.log(
          `✅ Azure API Response: ${response.status} ${response.config.url}`
        );
        return response;
      },
      (error) => {
        console.error(
          `❌ Azure API Error: ${error.response?.status} ${error.config?.url}`,
          error
        );

        if (error.response?.status === 401) {
          window.location.href = "/login";
        }

        return Promise.reject(error);
      }
    );
  }

  getStoredToken() {
    return localStorage.getItem("accessToken");
  }

  async prepareMatchingData(accessToken) {
    try {
      console.log("🚀 Preparing matching data via Azure Function...");

      const response = await this.apiClient.post(
        "/prepare-data",
        {
          timestamp: new Date().toISOString(),
          action: "prepare_matching_data",
        },
        {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        }
      );

      console.log("✅ Data preparation completed");
      return response.data;
    } catch (error) {
      console.error("❌ Failed to prepare matching data:", error);
      throw new Error(
        error.response?.data?.Message || "Failed to prepare data"
      );
    }
  }

  // === AZURE TABLE DATA METHODS ===

  async getAzureTableData(accessToken) {
    const columnsToSelect = [
      // Keys and essential fields
      "systemRowVersion",
      "documentNo",
      "lineNo",
      "PartitionKey",
      "RowKey",
      "Timestamp",

      // Fields for display in AzureFullTable.vue and comparison logic
      "selltoCustName_SalesHeader", // Used as the main customer name
      "shortName",
      "salespersonDimName",
      "custAppDimName",
      "regionDimName3", // For 'country' mapping
      "productGroup",
      "pCode",
      "chipName",
      "documentDate",
      "document",
      "quantity",
      "customerNo",
      "unitPrice",
      "lineAmount",
      "currencyRate",
      "salesPerUnit",
      "totalSales",
      "no", // Not a standard name, but could be important
      "description",
      "itemReferenceNo", // Important for similarity calculation
    ];

    try {
      console.log("📊 Fetching customer data from API...");
      const response = await this.apiClient.get("/customer-data/source", {
        headers: { Authorization: `Bearer ${accessToken}` },
        params: {
          select: columnsToSelect.join(',')
        }
      });

      if (
        response.data &&
        response.data.success === true &&
        response.data.code === 200 &&
        Array.isArray(response.data.data)
      ) {
        const customers = this.processCustomerData(response.data.data);
        console.log(
          `✅ Fetched and processed ${customers.length} customer records`
        );
        return customers;
      }

      console.error(
        "❌ Invalid response format from /api/customer-data/source",
        response.data
      );
      throw new Error("Invalid response format");
    } catch (error) {
      console.error("❌ Failed to fetch customer data:", error.message);
      if (error.response?.data) {
        console.error("Error details:", error.response.data);
      }
      console.log("🧪 Using mock Azure Table data for development");
      // การใช้ Mock data จะยังคงทำงานได้เหมือนเดิมหาก API call ล้มเหลว
      return this.getMockAzureData(); 
    }
  }

  processCustomerData(rawData) {
    return rawData.map((item) => {
      // 2. ทำการแปลงค่าในฟิลด์ที่ต้องการโดยใช้ฟังก์ชันที่ import มา
      const transformedSalesperson = transformPersonnelInfo(
        item.salespersonDimName
      );
      const transformedSellToCustomerName = transformPersonnelInfo(
        item.selltoCustName_SalesHeader
      );
      const transformedCustomerAppName = transformPersonnelInfo(
        item.custAppDimName
      );

      // 3. สร้าง object ใหม่โดยใช้ค่าที่แปลงแล้ว
      //    และจัดโครงสร้างข้อมูลให้พร้อมสำหรับฟังก์ชัน calculateSimilarity
      const finalItem = {
        // ...เก็บฟิลด์เดิมทั้งหมดจาก item...
        ...item,

        // --- เขียนทับฟิลด์ที่ต้องการด้วยค่าที่แปลงแล้ว ---
        salespersonDimName: transformedSalesperson,
        selltoCustName_SalesHeader: transformedSellToCustomerName,
        custAppDimName: transformedCustomerAppName,

        // --- สร้างฟิลด์มาตรฐานสำหรับฟังก์ชัน calculateSimilarity ---
        // ฟังก์ชัน calculateSimilarity จะมองหา 'name', 'email', 'company', 'country', 'industry'
        // เราจึงสร้างฟิลด์เหล่านี้ขึ้นมาโดยใช้ข้อมูลที่เหมาะสมที่สุด
        name: transformedSellToCustomerName, // ใช้ชื่อลูกค้าที่แปลงแล้วเป็น 'name' หลัก
        customerName: transformedSellToCustomerName, // ใช้เป็น 'customerName' ด้วยเพื่อความเข้ากันได้

        // ตัวอย่างการดึงอีเมลออกจาก 'salesperson' (ถ้าจำเป็น)
        email: this.extractEmail(transformedSalesperson), // ฟังก์ชัน helper สำหรับดึงอีเมล
        customerEmail: this.extractEmail(transformedSalesperson),

        company: transformedSellToCustomerName, // สมมติว่าชื่อลูกค้าคือชื่อบริษัท
        country: item.regionDimName3 || "", // สมมติว่า regionDimName3 คือประเทศ
        industry: item.custAppDimName || "", // สมมติว่า custAppDimName คือ industry

        // Uniqe key (เหมือนเดิม)
        id:
          item.systemRowVersion ||
          `${item.documentNo}_${item.lineNo}` ||
          crypto.randomUUID(),
      };

      return finalItem;
    });
  }

  extractEmail(transformedString) {
    if (!transformedString || typeof transformedString !== "string") return "";
    const match = transformedString.match(/\(([^)]+)\)/);
    return match ? match[1] : ""; // ถ้าเจอวงเล็บ, return สิ่งที่อยู่ในวงเล็บ (อีเมล)
  }

  async updateMergedData(payload) {
    try {
      console.log("📤 Sending data to update/create in merged table...");
      const response = await this.apiClient.post(
        "/customer-data/merged",
        payload
      );

      return response.data;
    } catch (error) {
      console.error("❌ Failed to update merged data:", error);

      throw new Error(
        error.response?.data?.message ||
          error.response?.data?.error ||
          "Failed to update data."
      );
    }
  }

  // === SHAREPOINT DATA METHODS ===

  async getSharePointOpportunities(accessToken) {
    try {
      console.log("📊 Fetching SharePoint opportunities...");

      const response = await this.apiClient.get("/sharepoint/opportunities", {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });

      if (response.data) {
        const opportunities = this.processSharePointOpportunities(
          response.data
        );
        console.log(
          `✅ Fetched ${opportunities.length} SharePoint opportunities`
        );
        return opportunities;
      } else {
        console.error(
          "❌ Invalid response format from SharePoint opportunities"
        );
        throw new Error("Invalid response format");
      }
    } catch (error) {
      console.error("❌ Failed to fetch SharePoint opportunities:", error);

      // Fallback to mock data for development
      console.log("🧪 Using mock SharePoint opportunities for development");
      return this.getMockSharePointOpportunities();
    }
  }

  async getSharePointContacts(accessToken) {
    try {
      console.log("📊 Fetching SharePoint contacts...");

      const response = await this.apiClient.get("/sharepoint/contacts", {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });

      if (response.data) {
        const contacts = this.processSharePointContacts(response.data);
        console.log(`✅ Fetched ${contacts.length} SharePoint contacts`);
        return contacts;
      } else {
        console.error("❌ Invalid response format from SharePoint contacts");
        throw new Error("Invalid response format");
      }
    } catch (error) {
      console.error("❌ Failed to fetch SharePoint contacts:", error);

      // Fallback to mock data for development
      console.log("🧪 Using mock SharePoint contacts for development");
      return this.getMockSharePointContacts();
    }
  }

  async performDataMatching(accessToken, matchingCriteria = {}) {
    try {
      console.log("🔄 Performing data matching between sources...");

      const response = await this.apiClient.post(
        "/data-matching",
        {
          criteria: matchingCriteria,
          timestamp: new Date().toISOString(),
          action: "perform_matching",
        },
        {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        }
      );

      if (response.data) {
        console.log("✅ Data matching completed successfully");
        return response.data;
      } else {
        throw new Error("Invalid response format");
      }
    } catch (error) {
      console.error("❌ Failed to perform data matching:", error);
      throw new Error(
        error.response?.data?.Message || "Failed to perform data matching"
      );
    }
  }

  // === DATA PROCESSING METHODS ===

  processSharePointOpportunities(rawData) {
    if (!Array.isArray(rawData)) {
      rawData = rawData.value || [rawData];
    }

    return rawData.map((item) => ({
      id: item.Id || item.id,
      title: item.Title,
      accountName: item.Account_x0020_Name,
      customerName: item.Customer_x0020_Name,
      distributorName: item.Distributor?.Title || item.DistributorName,
      stage: item.Stage,
      closeDate: item.Close_x0020_Date ? new Date(item.Close_x0020_Date) : null,
      amount: item.Amount || 0,
      probability: item.Probability || 0,
      description: item.Description,
      contactEmail: item.Contact_x0020_E_x002d_Mail,
      salesperson: item.Sales_x0020_Person,
      source: "sharepoint-opportunities",
      created: item.Created ? new Date(item.Created) : new Date(),
      modified: item.Modified ? new Date(item.Modified) : new Date(),
      author: item.Author?.Title || "Unknown",
      editor: item.Editor?.Title || "Unknown",
    }));
  }

  processSharePointContacts(rawData) {
    if (!Array.isArray(rawData)) {
      rawData = rawData.value || [rawData];
    }

    return rawData.map((item) => ({
      id: item.Id || item.id,
      fullName: item.Title,
      firstName: item.First_x0020_Name,
      lastName: item.Last_x0020_Name,
      email: item.E_x002d_mail,
      phone: item.Business_x0020_Phone,
      company: item.Company,
      jobTitle: item.Job_x0020_Title,
      department: item.Department,
      country: item.Country,
      city: item.City,
      address: item.Address,
      website: item.Website,
      source: "sharepoint-contacts",
      created: item.Created ? new Date(item.Created) : new Date(),
      modified: item.Modified ? new Date(item.Modified) : new Date(),
      author: item.Author?.Title || "Unknown",
      editor: item.Editor?.Title || "Unknown",
    }));
  }
}

export default new AzureService();
