using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DataMatchBackend.Authentication;
using DataMatchBackend.Models;
using DataMatchBackend.Services;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sic.Login;
using Microsoft.Extensions.Configuration;

namespace DataMatchBackend.Functions
{
    public class SharePointFunctions : BaseFunctionService
    {
        private readonly ISharePointService? _sharePointService;
        private readonly IConfiguration _configuration;

        public SharePointFunctions(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<SharePointFunctions> logger)
            : base(logger, serviceProvider)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _sharePointService = serviceProvider.GetService<ISharePointService>();

            LogServiceStatus();
        }

        // SharePointFunctions.cs

        [Function("GetSharePointContacts")]
        public async Task<HttpResponseData> GetSharePointContacts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sharepoint/contacts")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Endpoint `sharepoint/contacts` requested.");

                
                if (!IsServiceAvailable<ISharePointService>())
                {
                    _logger.LogWarning("GetSharePointContacts called but ISharePointService is not available in DI.");
                    return await CreateServiceUnavailableResponse(req, "SharePoint", "ENABLE_SHAREPOINT_SERVICE");
                }

               
                var userToken = ExtractUserToken(req);
                if (string.IsNullOrEmpty(userToken))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authorization header with a Bearer token is required.");
                }

                _logger.LogInformation("Calling SharePoint service to get opportunity list...");
                var result = await _sharePointService!.GetOpportunityListAsync(userToken); 
                
                if (result.Success)
                {
                    
                    var finalResponse = new ApiResponse<List<SharePointContact>>
                    {
                        Success = true,
                        Message = result.Message,
                        Data = result.Data ?? new List<SharePointContact>(), 
                        Count = result.Data?.Count ?? 0
                    };
                    finalResponse.WithMetadata("source", result.Source);

                    return await CreateOkResponse(req, finalResponse); 
                }
                else
                {
                    var statusCode = MapSharePointErrorCode(result.ErrorCode);
                    _logger.LogWarning("SharePoint service returned an error. Code: {ErrorCode}, Message: {Message}", result.ErrorCode, result.Message);
                    return await CreateErrorResponse(req, statusCode, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in GetSharePointContacts function");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An unexpected internal server error occurred.");
            }
        }

        [Function("GetSharePointLists")]
        public async Task<HttpResponseData> GetSharePointLists(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sharepoint/lists")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting available SharePoint lists");

                if (!IsServiceAvailable<ISharePointService>())
                {
                    return await CreateServiceUnavailableResponse(req, "SharePoint", "ENABLE_SHAREPOINT_SERVICE");
                }

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                var result = await _sharePointService!.GetAvailableListsAsync(authResult.UserToken ?? "");

                return result.Success
                    ? await CreateOkResponse(req, result.Data, result.Message)
                    : await CreateErrorResponse(req, HttpStatusCode.InternalServerError, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SharePoint lists");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to retrieve SharePoint lists");
            }
        }

        [Function("SearchSharePointContacts")]
        public async Task<HttpResponseData> SearchSharePointContacts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sharepoint/contacts/search")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Searching SharePoint contacts");

                if (!IsServiceAvailable<ISharePointService>())
                {
                    return await CreateServiceUnavailableResponse(req, "SharePoint", "ENABLE_SHAREPOINT_SERVICE");
                }

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                var searchRequest = await ParseSearchRequest(req);
                if (searchRequest == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Search query is required");
                }

                var result = await _sharePointService!.SearchOpportunitiesAsync(authResult.UserToken ?? "", searchRequest.Query);

                return result.Success
                    ? await CreateOkResponse(req, result.Data, result.Message)
                    : await CreateErrorResponse(req, HttpStatusCode.InternalServerError, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching SharePoint contacts");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Search failed");
            }
        }

        [Function("GetSharePointStatus")]
        public async Task<HttpResponseData> GetSharePointStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sharepoint/status")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting SharePoint configuration status");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                var configStatus = GetSharePointConfigurationStatus();
                return await CreateOkResponse(req, configStatus, "SharePoint status retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SharePoint status");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get SharePoint status");
            }
        }

        [Function("TestSharePointConnection")]
        public async Task<HttpResponseData> TestSharePointConnection(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sharepoint/test")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Testing SharePoint connection");

                if (!IsServiceAvailable<ISharePointService>())
                {
                    return await CreateServiceUnavailableResponse(req, "SharePoint", "ENABLE_SHAREPOINT_SERVICE");
                }

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                // ตรวจสอบ admin role
                if (!await ValidateAdminRole(req))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Forbidden, "Admin access required");
                }

                var connectionTest = await PerformConnectionTest(authResult.UserToken ?? "");
                var statusCode = connectionTest.success ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

                var response = req.CreateResponse(statusCode);
                await response.WriteAsJsonAsync(new ApiResponse<object>
                {
                    Code = (int)statusCode,
                    Message = connectionTest.message,
                    Data = connectionTest,
                    Success = connectionTest.success
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing SharePoint connection");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "SharePoint connection test failed");
            }
        }

        [Function("DiagnoseSharePointUser")]
        public async Task<HttpResponseData> DiagnoseSharePointUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sharepoint/diagnose-user")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("SharePoint user diagnostic request received");

                if (!IsServiceAvailable<ISharePointService>())
                {
                    return await CreateServiceUnavailableResponse(req, "SharePoint", "ENABLE_SHAREPOINT_SERVICE");
                }

                var authResult = await ValidateAuthenticationAsync(req);
                var result = await _sharePointService!.DiagnoseUserAsync(authResult.UserToken ?? "");

                return await CreateOkResponse(req, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DiagnoseSharePointUser");

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new UserDiagnosticResponse
                {
                    Success = false,
                    Message = "Diagnostic failed",
                    HasToken = !string.IsNullOrEmpty(ExtractUserToken(req))
                });
                return errorResponse;
            }
        }

        [Function("ValidateSharePointPermissions")]
        public async Task<HttpResponseData> ValidateSharePointPermissions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sharepoint/validate-permissions")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("SharePoint permission validation request received");

                if (!IsServiceAvailable<ISharePointService>())
                {
                    return await CreateServiceUnavailableResponse(req, "SharePoint", "ENABLE_SHAREPOINT_SERVICE");
                }

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                    await unauthorizedResponse.WriteAsJsonAsync(new PermissionValidationResponse
                    {
                        Success = false,
                        HasAccess = false,
                        Message = "User authentication required"
                    });
                    return unauthorizedResponse;
                }

                var result = await _sharePointService!.ValidateUserPermissionsAsync(authResult.UserToken ?? "");
                return await CreateOkResponse(req, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateSharePointPermissions");

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new PermissionValidationResponse
                {
                    Success = false,
                    HasAccess = false,
                    Message = "Permission validation failed"
                });
                return errorResponse;
            }
        }

        [Function("TestSharePointConnectionUser")]
        public async Task<HttpResponseData> TestSharePointConnectionUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sharepoint/test-connection-user")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("SharePoint connection test request received");

                if (!IsServiceAvailable<ISharePointService>())
                {
                    return await CreateServiceUnavailableResponse(req, "SharePoint", "ENABLE_SHAREPOINT_SERVICE");
                }

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                    await unauthorizedResponse.WriteAsJsonAsync(new ConnectionTestResponse
                    {
                        Success = false,
                        Message = "User authentication required for connection test"
                    });
                    return unauthorizedResponse;
                }

                var result = await _sharePointService!.TestConnectionAsync(authResult.UserToken ?? "");
                return await CreateOkResponse(req, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestSharePointConnectionUser");

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new ConnectionTestResponse
                {
                    Success = false,
                    Message = "Connection test failed"
                });
                return errorResponse;
            }
        }

        // Private helper methods
        private void LogServiceStatus()
        {
            var sharePointEnabled = bool.Parse(_configuration["ENABLE_SHAREPOINT_SERVICE"] ?? "false");
            var authEnabled = bool.Parse(_configuration["ENABLE_AUTH_SERVICE"] ?? "false");

            _logger.LogInformation("SharePointFunctions initialized:");
            _logger.LogInformation("  - SharePoint Service Enabled: {Enabled}", sharePointEnabled);
            _logger.LogInformation("  - SharePoint Service Available: {Available}", _sharePointService != null);
            _logger.LogInformation("  - Auth Service Enabled: {Enabled}", authEnabled);

            if (sharePointEnabled && _sharePointService == null)
            {
                _logger.LogWarning("ENABLE_SHAREPOINT_SERVICE is true but SharePointService is not available");
            }
        }

        private HttpStatusCode MapSharePointErrorCode(string? errorCode)
        {
            return errorCode switch
            {
                "SP_UNAUTHORIZED" => HttpStatusCode.Unauthorized,
                "SP_FORBIDDEN" => HttpStatusCode.Forbidden,
                "SP_NOT_FOUND" => HttpStatusCode.NotFound,
                _ => HttpStatusCode.InternalServerError
            };
        }

        private async Task<SharePointSearchRequest?> ParseSearchRequest(HttpRequestData req)
        {
            var requestBody = await req.ReadAsStringAsync();
            var searchRequest = JsonSerializer.Deserialize<SharePointSearchRequest>(requestBody ?? "{}");

            return string.IsNullOrEmpty(searchRequest?.Query) ? null : searchRequest;
        }

        private async Task<bool> ValidateAdminRole(HttpRequestData req)
        {
            var authenAccess = _serviceProvider.GetService<AuthenAccess>();
            if (authenAccess != null)
            {
                var userInfo = await authenAccess.ValidateTokenAsync(req);
                return userInfo?.HasRole(Constants.Roles.Admin) == true;
            }
            return true; // ถ้าไม่มี auth service ให้ผ่าน
        }

        private async Task<(bool success, string message, Dictionary<string, object> details)> PerformConnectionTest(string userToken)
        {
            try
            {
                var listsResult = await _sharePointService!.GetAvailableListsAsync(userToken);
                var contactsResult = await _sharePointService.GetOpportunityListAsync(userToken);

                return (
                    success: listsResult.Success && contactsResult.Success,
                    message: "SharePoint connection successful",
                    details: new Dictionary<string, object>
                    {
                        { "listsFound", listsResult.Data?.Count ?? 0 },
                        { "contactsFound", contactsResult.Data?.Count ?? 0 },
                        { "testTime", DateTime.UtcNow }
                    }
                );
            }
            catch (Exception spEx)
            {
                return (
                    success: false,
                    message: $"SharePoint connection failed: {spEx.Message}",
                    details: new Dictionary<string, object>
                    {
                        { "error", spEx.Message },
                        { "testTime", DateTime.UtcNow }
                    }
                );
            }
        }

        private SharePointConfigurationStatus GetSharePointConfigurationStatus()
        {
            var requiredSettings = new Dictionary<string, string>
            {
                { "SHAREPOINT_BASE_URL", _configuration["SHAREPOINT_BASE_URL"] ?? "" },
                { "SHAREPOINT_MAIN_SITE_ID", _configuration["SHAREPOINT_MAIN_SITE_ID"] ?? "" },
                { "GRAPH_CLIENT_ID", _configuration["GRAPH_CLIENT_ID"] ?? "" },
                { "GRAPH_CLIENT_SECRET", _configuration["GRAPH_CLIENT_SECRET"] ?? "" },
                { "GRAPH_TENANT_ID", _configuration["GRAPH_TENANT_ID"] ?? "" }
            };

            var listSettings = new Dictionary<string, string>
            {
                { "SHAREPOINT_OPPORTUNITY_LIST_ID", _configuration["SHAREPOINT_OPPORTUNITY_LIST_ID"] ?? "" },
                { "SHAREPOINT_CONTACT_LIST_ID", _configuration["SHAREPOINT_CONTACT_LIST_ID"] ?? "" },
                { "SHAREPOINT_PROSPECT_LIST_ID", _configuration["SHAREPOINT_PROSPECT_LIST_ID"] ?? "" }
            };

            var configuredSettings = requiredSettings.Where(s => !string.IsNullOrEmpty(s.Value)).Select(s => s.Key).ToList();
            var missingSettings = requiredSettings.Where(s => string.IsNullOrEmpty(s.Value)).Select(s => s.Key).ToList();
            var configuredLists = listSettings.Count(ls => !string.IsNullOrEmpty(ls.Value));

            return new SharePointConfigurationStatus
            {
                IsConfigured = missingSettings.Count == 0,
                ConfiguredSettings = configuredSettings,
                MissingSettings = missingSettings,
                ConfiguredLists = configuredLists,
                TotalListsConfigured = listSettings.Count,
                ServiceImplemented = _sharePointService != null,
                BaseUrl = _configuration["SHAREPOINT_BASE_URL"] ?? "",
                SitePath = _configuration["SHAREPOINT_SITE_PATH"] ?? "",
                TenantName = _configuration["SHAREPOINT_TENANT"] ?? ""
            };
        }
    }

    // Request/Response models (ไม่เปลี่ยนแปลง)
    public class SharePointSearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = "";

        [JsonPropertyName("listId")]
        public string? ListId { get; set; }

        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; } = 50;
    }

    public class SharePointConfigurationStatus
    {
        [JsonPropertyName("isConfigured")]
        public bool IsConfigured { get; set; } = false;

        [JsonPropertyName("serviceImplemented")]
        public bool ServiceImplemented { get; set; } = false;

        [JsonPropertyName("configuredSettings")]
        public List<string> ConfiguredSettings { get; set; } = new();

        [JsonPropertyName("missingSettings")]
        public List<string> MissingSettings { get; set; } = new();

        [JsonPropertyName("configuredLists")]
        public int ConfiguredLists { get; set; } = 0;

        [JsonPropertyName("totalListsConfigured")]
        public int TotalListsConfigured { get; set; } = 0;

        [JsonPropertyName("baseUrl")]
        public string BaseUrl { get; set; } = "";

        [JsonPropertyName("sitePath")]
        public string SitePath { get; set; } = "";

        [JsonPropertyName("tenantName")]
        public string TenantName { get; set; } = "";

        [JsonPropertyName("readyForImplementation")]
        public bool ReadyForImplementation => IsConfigured && !ServiceImplemented;

        [JsonPropertyName("setupInstructions")]
        public List<string> SetupInstructions => GetSetupInstructions();

        private List<string> GetSetupInstructions()
        {
            var instructions = new List<string>();

            if (!IsConfigured)
            {
                instructions.Add("Configure missing settings in local.settings.json");
                instructions.AddRange(MissingSettings.Select(s => $"- Add {s}"));
            }

            if (ConfiguredLists == 0)
            {
                instructions.Add("Configure at least one SharePoint list ID");
            }

            if (instructions.Count == 0)
            {
                instructions.Add("SharePoint integration is ready!");
            }

            return instructions;
        }
    }
}