using System.Text.Json.Serialization;

namespace DataMatchBackend.Models
{
    /// <summary>
    /// Response model for user diagnostic operations
    /// </summary>
    public class UserDiagnosticResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("hasToken")]
        public bool HasToken { get; set; }

        [JsonPropertyName("userInfo")]
        public UserInfo? UserInfo { get; set; }

        [JsonPropertyName("sharePointAccess")]
        public SharePointAccessInfo? SharePointAccess { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Response model for permission validation
    /// </summary>
    public class PermissionValidationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("hasAccess")]
        public bool HasAccess { get; set; }

        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; set; } = new();

        [JsonPropertyName("lists")]
        public List<ListInfo> Lists { get; set; } = new();

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Response model for connection testing
    /// </summary>
    public class ConnectionTestResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("connectionDetails")]
        public Dictionary<string, object> ConnectionDetails { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("responseTime")]
        public long ResponseTimeMs { get; set; }
    }

    /// <summary>
    /// User information from SharePoint
    /// </summary>
    public class UserInfo
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("loginName")]
        public string LoginName { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("userPrincipalName")]
        public string UserPrincipalName { get; set; } = "";
    }

    /// <summary>
    /// SharePoint access information for user
    /// </summary>
    public class SharePointAccessInfo
    {
        [JsonPropertyName("canAccessSites")]
        public bool CanAccessSites { get; set; }

        [JsonPropertyName("siteTitle")]
        public string SiteTitle { get; set; } = "";

        [JsonPropertyName("siteUrl")]
        public string SiteUrl { get; set; } = "";

        [JsonPropertyName("siteId")]
        public string SiteId { get; set; } = "";

        [JsonPropertyName("webId")]
        public string WebId { get; set; } = "";

        [JsonPropertyName("accessLevel")]
        public string AccessLevel { get; set; } = ""; // Read, Write, FullControl

        [JsonPropertyName("lastAccessed")]
        public DateTime? LastAccessed { get; set; }
    }

    /// <summary>
    /// SharePoint list information
    /// </summary>
    public class ListInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("itemCount")]
        public int ItemCount { get; set; }

        [JsonPropertyName("created")]
        public DateTime? Created { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTime? LastModified { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; } = "";

        [JsonPropertyName("canRead")]
        public bool CanRead { get; set; }

        [JsonPropertyName("canWrite")]
        public bool CanWrite { get; set; }
    }

    /// <summary>
    /// Standard SharePoint API response wrapper
    /// </summary>
    public class SharePointApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "Success";

        [JsonPropertyName("source")]
        public string Source { get; set; } = "sharepoint-user-context";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("userContext")]
        public UserContextInfo? UserContext { get; set; }

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        public static SharePointApiResponse<T> Ok(T data, string message = "Success")
        {
            return new SharePointApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                UserContext = new UserContextInfo()
            };
        }

        public static SharePointApiResponse<T> Error(string message, string? errorCode = null)
        {
            return new SharePointApiResponse<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                Data = default(T)
            };
        }

        public SharePointApiResponse<T> WithMetadata(string key, object value)
        {
            Metadata[key] = value;
            return this;
        }
        public SharePointApiResponse<T> WithUserContext(string userToken)
        {
            if (UserContext == null)
            {
                UserContext = new UserContextInfo();
            }

            UserContext.AccessMethod = "UserContext";
            UserContext.Scopes = new List<string> { "Sites.Read.All" };

            return this;
        }

    }

    /// <summary>
    /// User context information for tracking
    /// </summary>
    public class UserContextInfo
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = "";

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = "";

        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; } = "";

        [JsonPropertyName("accessMethod")]
        public string AccessMethod { get; set; } = "UserContext";

        [JsonPropertyName("tokenExpiry")]
        public DateTime? TokenExpiry { get; set; }

        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; } = new();
    }

    /// <summary>
    /// Error details for SharePoint operations
    /// </summary>
    public class SharePointError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("target")]
        public string Target { get; set; } = "";

        [JsonPropertyName("details")]
        public List<SharePointErrorDetail> Details { get; set; } = new();

        [JsonPropertyName("innerError")]
        public SharePointInnerError? InnerError { get; set; }
    }

    /// <summary>
    /// Detailed error information
    /// </summary>
    public class SharePointErrorDetail
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("target")]
        public string Target { get; set; } = "";
    }

    /// <summary>
    /// Inner error information
    /// </summary>
    public class SharePointInnerError
    {
        [JsonPropertyName("request-id")]
        public string RequestId { get; set; } = "";

        [JsonPropertyName("date")]
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bulk operation response
    /// </summary>
    public class BulkOperationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("successfulItems")]
        public int SuccessfulItems { get; set; }

        [JsonPropertyName("failedItems")]
        public int FailedItems { get; set; }

        [JsonPropertyName("errors")]
        public List<BulkOperationError> Errors { get; set; } = new();

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("processingTime")]
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Individual item error in bulk operations
    /// </summary>
    public class BulkOperationError
    {
        [JsonPropertyName("itemIndex")]
        public int ItemIndex { get; set; }

        [JsonPropertyName("itemId")]
        public string ItemId { get; set; } = "";

        [JsonPropertyName("error")]
        public string Error { get; set; } = "";

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; } = "";
    }
}