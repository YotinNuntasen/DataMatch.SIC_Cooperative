using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DataMatchBackend.Authentication;
using Microsoft.Extensions.DependencyInjection;
using DataMatchBackend.Models;
using DataMatchBackend.Services;
using System.Net;
using System.Text.Json;
using Sic.Login;

namespace DataMatchBackend.Functions
{
    public class ConfigFunctions : BaseFunctionService
    {
        public ConfigFunctions(ILogger<ConfigFunctions> logger, IServiceProvider serviceProvider) 
            : base(logger, serviceProvider)
        {
            LogServiceStatus();
        }

        [Function("GetConfiguration")]
        public async Task<HttpResponseData> GetConfiguration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting application configuration");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                var configuration = GetPublicConfiguration();
                return await CreateOkResponse(req, configuration, "Configuration retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get configuration");
            }
        }

        [Function("GetFeatureFlags")]
        public async Task<HttpResponseData> GetFeatureFlags(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config/features")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting feature flags");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                var featureFlags = GetFeatureFlags();
                return await CreateOkResponse(req, featureFlags, "Feature flags retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flags");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get feature flags");
            }
        }

        [Function("GetSystemLimits")]
        public async Task<HttpResponseData> GetSystemLimits(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config/limits")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting system limits");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                var systemLimits = GetSystemLimits();
                return await CreateOkResponse(req, systemLimits, "System limits retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system limits");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get system limits");
            }
        }

        [Function("UpdateConfiguration")]
        public async Task<HttpResponseData> UpdateConfiguration(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "config/update")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Updating configuration");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                var authenAccess = _serviceProvider.GetService<AuthenAccess>();
                if (authenAccess != null)
                {
                    var userInfo = await authenAccess.ValidateTokenAsync(req);
                    if (userInfo == null || !userInfo.HasRole(Constants.Roles.Admin))
                    {
                        return await CreateErrorResponse(req, HttpStatusCode.Forbidden, "Admin access required");
                    }

                    var updateRequest = await ParseUpdateRequest(req);
                    if (updateRequest == null)
                    {
                        return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid configuration update request");
                    }

                    _logger.LogInformation("Configuration update requested by {Email}", userInfo.Email);

                    var updateData = new
                    {
                        updatedBy = userInfo.Email,
                        timestamp = DateTime.UtcNow,
                        changes = updateRequest.Changes?.Count ?? 0
                    };

                    return await CreateOkResponse(req, updateData, "Configuration update queued (restart required)");
                }
                else
                {
                    var updateData = new
                    {
                        updatedBy = "development",
                        timestamp = DateTime.UtcNow,
                        authDisabled = true
                    };

                    return await CreateOkResponse(req, updateData, "Configuration update accepted (development mode)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Configuration update failed");
            }
        }

        // Private helper methods
        private void LogServiceStatus()
        {
            var authEnabled = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_AUTH_SERVICE") ?? "false");
            _logger.LogInformation("ConfigFunctions initialized:");
            _logger.LogInformation("  - Auth Service Enabled: {Enabled}", authEnabled);
            _logger.LogInformation("  - AuthenAccess Available: {Available}", _serviceProvider.GetService<AuthenAccess>() != null);
        }

        private async Task<ConfigurationUpdateRequest?> ParseUpdateRequest(HttpRequestData req)
        {
            var requestBody = await req.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ConfigurationUpdateRequest>(requestBody ?? "{}");
        }

        private object GetPublicConfiguration()
        {
            return new
            {
                application = new
                {
                    name = "Data Match Portal",
                    version = Environment.GetEnvironmentVariable("API_VERSION") ?? "1.0.0",
                    environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development"
                },
                matching = new
                {
                    similarityThresholds = new
                    {
                        autoMatch = double.Parse(Environment.GetEnvironmentVariable("SIMILARITY_THRESHOLD_AUTO") ?? "80"),
                        suggestions = double.Parse(Environment.GetEnvironmentVariable("SIMILARITY_THRESHOLD_SUGGEST") ?? "60")
                    },
                    maxSuggestionsPerRecord = int.Parse(Environment.GetEnvironmentVariable("MAX_SUGGESTIONS_PER_RECORD") ?? "5"),
                    algorithms = new[] { "weighted", "simple", "advanced" },
                    enableAiMatching = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_AI_MATCHING") ?? "true")
                },
                validation = new
                {
                    enableDataValidation = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_DATA_VALIDATION") ?? "true"),
                    validateEmailFormat = bool.Parse(Environment.GetEnvironmentVariable("VALIDATE_EMAIL_FORMAT") ?? "true"),
                    validatePhoneFormat = bool.Parse(Environment.GetEnvironmentVariable("VALIDATE_PHONE_FORMAT") ?? "false"),
                    requireCustomerName = bool.Parse(Environment.GetEnvironmentVariable("REQUIRE_CUSTOMER_NAME") ?? "true")
                },
                export = new
                {
                    enableCsvExport = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_CSV_EXPORT") ?? "true"),
                    enableExcelExport = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_EXCEL_EXPORT") ?? "false"),
                    maxExportRecords = int.Parse(Environment.GetEnvironmentVariable("MAX_EXPORT_RECORDS") ?? "10000"),
                    includeMetadata = bool.Parse(Environment.GetEnvironmentVariable("EXPORT_INCLUDE_METADATA") ?? "true")
                },
                ui = new
                {
                    enableSwagger = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_SWAGGER") ?? "true"),
                    theme = "light",
                    itemsPerPage = 50
                },
                services = new
                {
                    dataService = _serviceProvider.GetService<IDataService>() != null,
                    matchingService = _serviceProvider.GetService<ISimilarityService>() != null,
                    sharePointService = _serviceProvider.GetService<ISharePointService>() != null,
                    authService = _serviceProvider.GetService<AuthenAccess>() != null
                }
            };
        }

        private Dictionary<string, bool> GetFeatureFlags()
        {
            return new Dictionary<string, bool>
            {
                { "advancedMatching", bool.Parse(Environment.GetEnvironmentVariable("FEATURE_ADVANCED_MATCHING") ?? "false") },
                { "bulkOperations", bool.Parse(Environment.GetEnvironmentVariable("FEATURE_BULK_OPERATIONS") ?? "true") },
                { "dataMigration", bool.Parse(Environment.GetEnvironmentVariable("FEATURE_DATA_MIGRATION") ?? "false") },
                { "analytics", bool.Parse(Environment.GetEnvironmentVariable("FEATURE_ANALYTICS") ?? "false") },
                { "notifications", bool.Parse(Environment.GetEnvironmentVariable("FEATURE_NOTIFICATIONS") ?? "false") },
                { "sharePointIntegration", _serviceProvider.GetService<ISharePointService>() != null },
                { "authenticationEnabled", _serviceProvider.GetService<AuthenAccess>() != null },
                { "similarityMatching", _serviceProvider.GetService<ISimilarityService>() != null },
                { "cacheEnabled", bool.Parse(Environment.GetEnvironmentVariable("ENABLE_REDIS_CACHE") ?? "false") },
                { "rateLimiting", bool.Parse(Environment.GetEnvironmentVariable("ENABLE_RATE_LIMITING") ?? "true") },
                { "auditLogging", bool.Parse(Environment.GetEnvironmentVariable("ENABLE_AUDIT_LOGGING") ?? "false") },
                { "detailedLogging", bool.Parse(Environment.GetEnvironmentVariable("ENABLE_DETAILED_LOGGING") ?? "false") }
            };
        }

        private object GetSystemLimits()
        {
            return new
            {
                performance = new
                {
                    maxConcurrentRequests = int.Parse(Environment.GetEnvironmentVariable("MAX_CONCURRENT_REQUESTS") ?? "5"),
                    requestTimeoutSeconds = int.Parse(Environment.GetEnvironmentVariable("REQUEST_TIMEOUT_SECONDS") ?? "300"),
                    batchProcessingSize = int.Parse(Environment.GetEnvironmentVariable("BATCH_PROCESSING_SIZE") ?? "25")
                },
                rateLimiting = new
                {
                    requestsPerMinute = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_REQUESTS_PER_MINUTE") ?? "30"),
                    enabled = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_RATE_LIMITING") ?? "true")
                },
                dataLimits = new
                {
                    maxExportRecords = int.Parse(Environment.GetEnvironmentVariable("MAX_EXPORT_RECORDS") ?? "5000"),
                    maxBulkUpdateRecords = 1000,
                    maxSuggestionsPerRecord = int.Parse(Environment.GetEnvironmentVariable("MAX_SUGGESTIONS_PER_RECORD") ?? "3"),
                    maxSearchResults = 500
                },
                cache = new
                {
                    enabled = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_REDIS_CACHE") ?? "false"),
                    expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("CACHE_EXPIRY_MINUTES") ?? "30")
                },
                sharePoint = new
                {
                    batchSize = int.Parse(Environment.GetEnvironmentVariable("SHAREPOINT_BATCH_SIZE") ?? "10"),
                    retryEnabled = bool.Parse(Environment.GetEnvironmentVariable("SHAREPOINT_ENABLE_RETRY") ?? "true"),
                    configured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SHAREPOINT_MAIN_SITE_ID"))
                }
            };
        }
    }

    public class ConfigurationUpdateRequest
    {
        [JsonPropertyName("changes")]
        public Dictionary<string, object>? Changes { get; set; }
        
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";
        
        [JsonPropertyName("applyImmediately")]
        public bool ApplyImmediately { get; set; } = false;
    }
}