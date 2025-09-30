import * as msal from "@azure/msal-browser";

/**
 * Parse MSAL authentication errors
 */
export function parseAuthError(error) {
  if (!(error instanceof msal.BrowserAuthError)) {
    return "An unexpected error occurred. Please try again.";
  }

  const errorMessages = {
    user_cancelled: "Login was cancelled by user.",
    popup_window_error: "Popup blocked. Please allow popups for this site.",
    consent_required: "Consent required. Contact your IT department.",
    interaction_in_progress: "Another authentication in progress. Please wait.",
  };

  for (const [code, message] of Object.entries(errorMessages)) {
    if (error.errorCode?.includes(code) || error.message?.includes(code.toUpperCase())) {
      return message;
    }
  }

  if (error.message?.includes("AADSTS65001")) {
    return errorMessages.consent_required;
  }

  return `Authentication failed: ${error.errorCode || 'Unknown error'}`;
}