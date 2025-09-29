import * as msal from "@azure/msal-browser";


const msalConfig = {
  auth: {
    clientId: "7281d6d6-29d6-40cb-87d2-1bc6eb678cb3",
    authority: "https://login.microsoftonline.com/f21d466c-a8db-4dbe-9a97-4e79d654a7f8",
    redirectUri: "http://localhost:8080", 
  },
  cache: {
    cacheLocation: "localStorage",
    storeAuthStateInCookie: false,
  },
  system: {
   
    loggerOptions: { 
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        switch (level) {
          case msal.LogLevel.Error:
            console.error(message);
            return;
          case msal.LogLevel.Warning:
            console.warn(message);
            return;
        }
      },
      piiLoggingEnabled: false
    },
  },
};

const msalInstance = new msal.PublicClientApplication(msalConfig);

const loginRequest = {
  scopes: ["openid", "profile", "User.Read"],
};

const sharepointTokenRequest = {
  scopes: ["https://sicth.sharepoint.com/AllSites.Read"],
};



const state = {
  accessToken: localStorage.getItem("accessToken") || null, 
  account: JSON.parse(localStorage.getItem("account")) || null,
  isInitialized: false,
};

const getters = {
  isAuthenticated: (state) => !!state.account,
  account: (state) => state.account,
  userEmail: (state) => state.account?.username || "",
  userName: (state) => state.account?.name || "",
  isDevelopmentMode: () => process.env.NODE_ENV === 'development',
};

const mutations = {
  SET_USER(state, { accessToken, account }) {
    state.accessToken = accessToken;
    state.account = account;
    localStorage.setItem("accessToken", accessToken);
    localStorage.setItem("account", JSON.stringify(account));
  },
  SET_INITIALIZED(state, initialized) {
    state.isInitialized = initialized;
  },
  CLEAR_USER(state) {
    state.accessToken = null;
    state.account = null;
    msalInstance.clearCache(); // เคลียร์ cache ของ msal ด้วย
    localStorage.removeItem("accessToken");
    localStorage.removeItem("account");
  },
};

const actions = {
  async initialize({ commit }) {
    if (state.isInitialized) return;
    
    await msalInstance.initialize();
    
    await msalInstance.handleRedirectPromise();
    
    const accounts = msalInstance.getAllAccounts();
    if (accounts.length > 0) {
      msalInstance.setActiveAccount(accounts[0]);
    
      commit("SET_USER", { 
          accessToken: localStorage.getItem('accessToken'), // 
          account: accounts[0] 
      });
    }
    
    commit("SET_INITIALIZED", true);
  },

  async login({ commit, dispatch }) {
    dispatch("setLoading", true, { root: true });
    try {
      if (!state.isInitialized) {
        await dispatch("initialize");
      }
      
      const authResult = await msalInstance.loginPopup(loginRequest);
      
      msalInstance.setActiveAccount(authResult.account);
      
      commit("SET_USER", {
        accessToken: authResult.accessToken,
        account: authResult.account,
      });
      
      return authResult;

    } catch (error) {
      console.error("Login failed:", error);
      let errorMessage = "Login failed. Please try again.";
      if (error instanceof msal.BrowserAuthError) {
        if (error.errorCode.includes("user_cancelled")) {
          errorMessage = "Login was cancelled.";
        } else if (error.errorCode.includes("popup_window_error")) {
          errorMessage = "Popup blocked. Please allow popups for this site.";
        } else if (error.errorCode.includes("consent_required") || error.message.includes("AADSTS65001")) {
           errorMessage = "Consent is required from an administrator. Please contact your IT department.";
        }
      }
      dispatch("setError", errorMessage, { root: true });
      throw error;
    } finally {
      dispatch("setLoading", false, { root: true });
    }
  },

  async logout({ commit }) {
    const account = msalInstance.getActiveAccount();
    if (account) {
      await msalInstance.logoutPopup({ account });
    }
    commit('CLEAR_USER');
    // ไม่ต้อง redirect เอง ปล่อยให้ router guard จัดการ
  },
  
  /**
   * Action กลางสำหรับขอ Access Token สำหรับ SharePoint
   * จะพยายามขอแบบเงียบๆ ก่อนเสมอ ถ้าไม่สำเร็จจะใช้ Popup
   * นี่คือ action ที่ service อื่นๆ ควรเรียกใช้
   */
  async acquireSharePointToken({ state, dispatch }) {
    if (!state.isInitialized) {
      await dispatch("initialize");
    }

    const account = msalInstance.getActiveAccount();
    if (!account) {
      console.warn("No active account found. Attempting to log in.");
      await dispatch('login');
      // หลังจาก login สำเร็จ, account จะถูก set ใหม่
      // เราต้อง get account อีกครั้ง
      const newAccount = msalInstance.getActiveAccount();
      if (!newAccount) throw new Error("Login failed, cannot acquire token.");
      
      // ตั้งค่า request ใหม่ด้วย account ใหม่
      const request = { ...sharepointTokenRequest, account: newAccount };
      // ขอ token ด้วย popup เลยเพราะเพิ่ง login มา
      const response = await msalInstance.acquireTokenPopup(request);
      return response.accessToken;
    }
    
    const request = { ...sharepointTokenRequest, account };

    try {
      // 1. ลองขอแบบเงียบๆ ก่อนเสมอ
      const response = await msalInstance.acquireTokenSilent(request);
      return response.accessToken;
    } catch (error) {
      // 2. ถ้าแบบเงียบไม่สำเร็จ
      console.warn("Silent token acquisition failed, falling back to popup.", error);
      if (error instanceof msal.InteractionRequiredAuthError) {
        try {
          const response = await msalInstance.acquireTokenPopup(request);
          return response.accessToken;
        } catch (popupError) {
          console.error("Popup token acquisition failed.", popupError);
          dispatch("setError", "Could not get authorization token. Please try logging in again.", { root: true });
          throw popupError;
        }
      } else {
        console.error("Unhandled token acquisition error.", error);
        throw error;
      }
    }
  },
};

export default {
  namespaced: true,
  state,
  getters,
  mutations,
  actions,
};