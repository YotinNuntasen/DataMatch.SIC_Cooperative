// config/msalConfig.js

import * as msal from "@azure/msal-browser";

// Environment-based configuration
const getRedirectUri = () => {
  const baseUrl = process.env.NODE_ENV === 'production' 
    ? process.env.VUE_APP_BASE_URL || window.location.origin
    : "http://localhost:8080";
  return baseUrl;
};

// MSAL Configuration
export const msalConfig = {
  auth: {
    clientId: process.env.VUE_APP_CLIENT_ID || "7281d6d6-29d6-40cb-87d2-1bc6eb678cb3",
    authority: process.env.VUE_APP_AUTHORITY || "https://login.microsoftonline.com/f21d466c-a8db-4dbe-9a97-4e79d654a7f8",
    redirectUri: "https://sicwebapp001.z23.web.core.windows.net/nbo-matching/",
    //https://webapp.sic.co.th/nbo-matching/
  },
  cache: {
    cacheLocation: "localStorage",
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: { 
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        
        const logMethods = {
          [msal.LogLevel.Error]: console.error,
          [msal.LogLevel.Warning]: console.warn,
          [msal.LogLevel.Info]: console.info,
          [msal.LogLevel.Verbose]: console.debug,
        };
        
        const logMethod = logMethods[level];
        if (logMethod) {
          logMethod(`[MSAL] ${message}`);
        }
      },
      piiLoggingEnabled: false
    },
  },
};

// Request configurations
export const authRequests = {
  login: {
    scopes: ["openid", "profile", "User.Read"],
  },
  sharepoint: {
    scopes: [
      process.env.VUE_APP_SHAREPOINT_SCOPE || "https://sicth.sharepoint.com/AllSites.Read"
    ],
  },
};

// Storage keys
export const storageKeys = {
  ACCESS_TOKEN: "accessToken",
  ACCOUNT: "account",
  REFRESH_TOKEN: "refreshToken",
};