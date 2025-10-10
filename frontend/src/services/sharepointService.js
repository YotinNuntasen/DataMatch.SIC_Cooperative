import axios from "axios";
import store from "../store";

class SharePointService {
  constructor() {
    this.baseURL = 'https://nbo-matching.azurewebsites.net';
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

  // ===== แก้ไข getMockSharePointData ให้มีข้อมูลที่หลากหลายขึ้น =====
  getMockSharePointData() {
    const mockData = [];
    const count = 30;

    for (let i = 1; i <= count; i++) {
      const firstName = this.getRandomFirstName();
      const lastName = this.getRandomLastName();
      const company = `${this.getRandomCompanyName()} ${this.getRandomCompanySuffix()}`;

      mockData.push({
        id: `sp-${i}`,
        name: `${firstName} ${lastName}`,
        email: `${firstName.toLowerCase()}.${lastName.toLowerCase()}@${this.getRandomEmailDomain()}`,
        company: company,
        country: this.getRandomCountry(),
        department: this.getRandomDepartment(),
        phone: this.generatePhoneNumber(),
        jobTitle: this.getRandomJobTitle(),
        industry: this.getRandomIndustry(),
        website: `https://www.${company.toLowerCase().replace(/\s+/g, "")}.com`,
        address: this.getRandomAddress(),
        notes: this.getRandomNotes(),
        source: "sharepoint-mock",
        created: new Date(
          Date.now() - Math.random() * 180 * 24 * 60 * 60 * 1000
        ),
        modified: new Date(
          Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000
        ),
      });
    }

    console.log(`🧪 Generated ${mockData.length} mock SharePoint records`);
    return mockData;
  }

  getRandomFirstName() {
    const names = [
      "Alexander",
      "Emma",
      "Christopher",
      "Sophie",
      "Ryan",
      "Olivia",
      "Jordan",
      "Isabella",
      "Taylor",
      "Ava",
      "Morgan",
      "Charlotte",
      "Casey",
      "Amelia",
      "Riley",
      "Harper",
      "Avery",
      "Ella",
      "Quinn",
      "Scarlett",
      "Sage",
      "Grace",
      "River",
      "Chloe",
    ];
    return names[Math.floor(Math.random() * names.length)];
  }

  getRandomLastName() {
    const names = [
      "Thompson",
      "Rodriguez",
      "Martinez",
      "Gonzalez",
      "Wilson",
      "Anderson",
      "Thomas",
      "Jackson",
      "White",
      "Harris",
      "Martin",
      "Garcia",
      "Robinson",
      "Clark",
      "Lewis",
      "Lee",
      "Walker",
      "Hall",
      "Allen",
      "Young",
      "King",
      "Wright",
    ];
    return names[Math.floor(Math.random() * names.length)];
  }

  getRandomCompanyName() {
    const names = [
      "Global",
      "International",
      "Dynamic",
      "Innovative",
      "Advanced",
      "Premier",
      "Elite",
      "Professional",
      "Strategic",
      "Creative",
      "Digital",
      "Modern",
      "Future",
      "Smart",
      "Efficient",
      "Reliable",
      "Trusted",
      "Leading",
      "Pioneer",
      "Visionary",
      "Nextwaves Industries",
    ];
    return names[Math.floor(Math.random() * names.length)];
  }

  getRandomCompanySuffix() {
    const suffixes = [
      "Solutions",
      "Systems",
      "Technologies",
      "Industries",
      "Group",
      "Corporation",
      "Enterprises",
      "Partners",
      "Associates",
      "Consulting",
      "Services",
      "Holdings",
      "Dynamics",
      "Innovation",
      "Ventures",
    ];
    return suffixes[Math.floor(Math.random() * suffixes.length)];
  }

  getRandomEmailDomain() {
    const domains = [
      "company.com",
      "business.org",
      "enterprise.net",
      "corp.com",
      "solutions.com",
      "systems.io",
      "tech.com",
      "global.org",
      "international.com",
      "services.net",
      "group.com",
      "industries.com",
    ];
    return domains[Math.floor(Math.random() * domains.length)];
  }

  getRandomCountry() {
    const countries = [
      "United States",
      "Canada",
      "United Kingdom",
      "Germany",
      "France",
      "Australia",
      "Japan",
      "Singapore",
      "Netherlands",
      "Sweden",
      "Switzerland",
      "Denmark",
      "Norway",
      "Finland",
      "Belgium",
      "Austria",
      "Ireland",
      "New Zealand",
      "South Korea",
      "Italy",
    ];
    return countries[Math.floor(Math.random() * countries.length)];
  }

  getRandomDepartment() {
    const departments = [
      "Information Technology",
      "Marketing",
      "Sales",
      "Human Resources",
      "Finance",
      "Operations",
      "Research & Development",
      "Customer Service",
      "Quality Assurance",
      "Business Development",
      "Product Management",
      "Engineering",
      "Design",
      "Legal",
      "Procurement",
      "Strategy",
    ];
    return departments[Math.floor(Math.random() * departments.length)];
  }

  getRandomJobTitle() {
    const titles = [
      "Senior Manager",
      "Director",
      "Vice President",
      "Team Lead",
      "Senior Consultant",
      "Project Manager",
      "Business Analyst",
      "Technical Specialist",
      "Account Manager",
      "Product Owner",
      "Solutions Architect",
      "Senior Developer",
      "Marketing Manager",
      "Sales Director",
      "Operations Manager",
      "Strategy Consultant",
    ];
    return titles[Math.floor(Math.random() * titles.length)];
  }

  getRandomIndustry() {
    const industries = [
      "Technology",
      "Healthcare",
      "Financial Services",
      "Manufacturing",
      "Retail",
      "Education",
      "Telecommunications",
      "Energy",
      "Automotive",
      "Aerospace",
      "Biotechnology",
      "Media & Entertainment",
      "Real Estate",
      "Transportation",
      "Consulting",
      "Government",
    ];
    return industries[Math.floor(Math.random() * industries.length)];
  }

  generatePhoneNumber() {
    const areaCodes = ["212", "415", "617", "713", "404", "602", "503", "206"];
    const areaCode = areaCodes[Math.floor(Math.random() * areaCodes.length)];
    const exchange = Math.floor(Math.random() * 900) + 100;
    const number = Math.floor(Math.random() * 9000) + 1000;

    return `+1-${areaCode}-${exchange}-${number}`;
  }

  getRandomAddress() {
    const streets = [
      "Main Street",
      "Oak Avenue",
      "Park Road",
      "First Street",
      "Second Avenue",
      "Elm Street",
      "Washington Boulevard",
      "Lincoln Drive",
      "Madison Avenue",
      "Broadway",
      "Market Street",
      "Church Street",
      "Mill Road",
      "High Street",
    ];

    const streetNumber = Math.floor(Math.random() * 9999) + 1;
    const street = streets[Math.floor(Math.random() * streets.length)];

    return `${streetNumber} ${street}`;
  }

  getRandomNotes() {
    const notes = [
      "Key decision maker for IT initiatives",
      "Interested in cloud solutions",
      "Budget approved for Q4",
      "Evaluating multiple vendors",
      "Long-term strategic partnership potential",
      "Previous customer with positive experience",
      "Referred by existing client",
      "Attended recent trade show",
      "Downloaded multiple white papers",
      "Engaged with social media content",
    ];
    return notes[Math.floor(Math.random() * notes.length)];
  }
}

export default new SharePointService();