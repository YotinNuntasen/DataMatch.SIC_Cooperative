using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DataMatchBackend.Models;
using System.Net;
using System.Text.Json;
using DataMatchBackend.Services;
using System.Diagnostics;

namespace DataMatchBackend.Functions
{
    public class HealthFunctions : BaseFunctionService
    {
        private readonly IDataService? _dataService;

        public HealthFunctions(IServiceProvider serviceProvider, ILogger<HealthFunctions> logger) 
            : base(logger, serviceProvider)
        {
            _dataService = serviceProvider.GetService<IDataService>();
            LogServiceStatus();
        }

        [Function("Health")]
        public async Task<HttpResponseData> HealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Health check requested");

                var healthStatus = await CheckSystemHealth();
                var statusCode = healthStatus.IsHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

                var response = req.CreateResponse(statusCode);
                await response.WriteAsJsonAsync(healthStatus);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return await CreateHealthErrorResponse(req, ex);
            }
        }

        [Function("HealthDetailed")]
        public async Task<HttpResponseData> DetailedHealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/detailed")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Detailed health check requested");

                var detailedHealth = await CheckDetailedHealth();
                var statusCode = detailedHealth.IsHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

                var response = req.CreateResponse(statusCode);
                await response.WriteAsJsonAsync(detailedHealth);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed health check failed");
                return await CreateHealthErrorResponse(req, ex);
            }
        }

        [Function("SystemInfo")]
        public async Task<HttpResponseData> GetSystemInfo(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "system/info")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("System information requested");

                var systemInfo = await GetSystemInformation();
                return await CreateOkResponse(req, systemInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system information");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get system information");
            }
        }

        [Function("HealthAdmin")]
        public async Task<HttpResponseData> AdminHealthCheck(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "health/admin")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Admin health check requested");

                var userToken = ExtractUserToken(req);
                var adminHealth = await CheckAdminHealth(userToken);
                var statusCode = adminHealth.IsHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

                var response = req.CreateResponse(statusCode);
                await response.WriteAsJsonAsync(adminHealth);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin health check failed");
                return await CreateHealthErrorResponse(req, ex);
            }
        }

        // Private helper methods
        private void LogServiceStatus()
        {
            _logger.LogInformation("HealthFunctions initialized:");
            _logger.LogInformation("  - DataService: {Available}", _dataService != null ? "Available" : "Not Available");
        }

        private async Task<HttpResponseData> CreateHealthErrorResponse(HttpRequestData req, Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await errorResponse.WriteAsJsonAsync(new
            {
                status = "Error",
                message = ex.Message,
                timestamp = DateTime.UtcNow,
                isHealthy = false
            });
            return errorResponse;
        }

        private async Task<HealthStatus> CheckSystemHealth()
        {
            var healthStatus = new HealthStatus
            {
                Status = "OK",
                Timestamp = DateTime.UtcNow,
                Version = GetVersion(),
                Environment = GetEnvironment()
            };

            var checks = new List<Task<ComponentHealth>>
            {
                CheckAzureTableHealth(),
                CheckConfigurationHealth(),
                CheckAuthenticationHealth(),
                CheckServicesHealth()
            };

            var results = await Task.WhenAll(checks);
            healthStatus.Components = results.ToList();

            healthStatus.IsHealthy = results.All(r => r.IsHealthy);
            if (!healthStatus.IsHealthy)
            {
                healthStatus.Status = "Unhealthy";
            }

            return healthStatus;
        }

        private async Task<DetailedHealthStatus> CheckDetailedHealth()
        {
            var detailedHealth = new DetailedHealthStatus
            {
                Status = "OK",
                Timestamp = DateTime.UtcNow,
                Version = GetVersion(),
                Environment = GetEnvironment(),
                IsHealthy = true
            };

            var components = new List<ComponentHealth>
            {
                await CheckAzureTableHealth(),
                await CheckConfigurationHealth(),
                await CheckAuthenticationHealth(),
                await CheckServicesHealth(),
                CheckPerformanceHealth()
            };

            if (IsServiceAvailable<ISharePointService>())
            {
                components.Add(await CheckSharePointHealth());
            }

            detailedHealth.Components = components;
            detailedHealth.Statistics = await GetSystemStatistics();
            detailedHealth.IsHealthy = components.All(c => c.IsHealthy);

            if (!detailedHealth.IsHealthy)
            {
                detailedHealth.Status = "Unhealthy";
            }

            return detailedHealth;
        }

        private async Task<DetailedHealthStatus> CheckAdminHealth(string? userToken)
        {
            var adminHealth = await CheckDetailedHealth();

            if (!string.IsNullOrEmpty(userToken))
            {
                var sharePointDetailedHealth = await CheckSharePointHealthDetailed(userToken);
                adminHealth.Components.Add(sharePointDetailedHealth);
            }

            var sharePointConfigHealth = CheckSharePointConfiguration();
            adminHealth.Components.Add(sharePointConfigHealth);

            adminHealth.IsHealthy = adminHealth.Components.All(c => c.IsHealthy);
            if (!adminHealth.IsHealthy)
            {
                adminHealth.Status = "Partial";
            }

            return adminHealth;
        }

        private async Task<ComponentHealth> CheckAzureTableHealth()
        {
            var componentHealth = new ComponentHealth
            {
                Name = "Azure Table Storage",
                Type = "Database"
            };

            try
            {
                if (_dataService == null)
                {
                    componentHealth.IsHealthy = false;
                    componentHealth.Status = "Disabled";
                    componentHealth.Message = "DataService is not enabled";
                    componentHealth.Details = new Dictionary<string, object>
                    {
                        { "ServiceEnabled", false },
                        { "Reason", "ENABLE_DATA_SERVICE is false or service not registered" }
                    };
                    return componentHealth;
                }

                var customers = await _dataService.GetAllCustomersAsync();

                componentHealth.IsHealthy = true;
                componentHealth.Status = "OK";
                componentHealth.ResponseTime = TimeSpan.FromMilliseconds(100);
                componentHealth.Details = new Dictionary<string, object>
                {
                    { "RecordCount", customers.Count },
                    { "TableName", Environment.GetEnvironmentVariable("TableName") ?? "mergentbowitherp" },
                    { "ConnectionStatus", "Connected" },
                    { "ServiceEnabled", true }
                };
            }
            catch (Exception ex)
            {
                componentHealth.IsHealthy = false;
                componentHealth.Status = "Error";
                componentHealth.Message = ex.Message;
                componentHealth.Details = new Dictionary<string, object>
                {
                    { "ConnectionStatus", "Failed" },
                    { "Error", ex.Message }
                };
            }

            return componentHealth;
        }

        private async Task<ComponentHealth> CheckConfigurationHealth()
        {
            var componentHealth = new ComponentHealth
            {
                Name = "Configuration",
                Type = "Configuration"
            };

            await Task.Delay(1);

            try
            {
                var requiredSettings = new[]
                {
                    "AzureWebJobsStorage",
                    "TableName",
                    "FUNCTIONS_WORKER_RUNTIME"
                };

                var missingSettings = new List<string>();
                var presentSettings = new List<string>();

                foreach (var setting in requiredSettings)
                {
                    var value = Environment.GetEnvironmentVariable(setting);
                    if (string.IsNullOrEmpty(value))
                    {
                        missingSettings.Add(setting);
                    }
                    else
                    {
                        presentSettings.Add(setting);
                    }
                }

                componentHealth.IsHealthy = !missingSettings.Any();
                componentHealth.Status = componentHealth.IsHealthy ? "OK" : "Warning";
                componentHealth.Details = new Dictionary<string, object>
                {
                    { "RequiredSettings", requiredSettings.Length },
                    { "PresentSettings", presentSettings.Count },
                    { "MissingSettings", missingSettings },
                    { "Environment", GetEnvironment() }
                };

                if (!componentHealth.IsHealthy)
                {
                    componentHealth.Message = $"Missing required settings: {string.Join(", ", missingSettings)}";
                }
            }
            catch (Exception ex)
            {
                componentHealth.IsHealthy = false;
                componentHealth.Status = "Error";
                componentHealth.Message = ex.Message;
            }

            return componentHealth;
        }

        private async Task<ComponentHealth> CheckAuthenticationHealth()
        {
            var componentHealth = new ComponentHealth
            {
                Name = "Authentication",
                Type = "Security"
            };

            await Task.Delay(1);

            try
            {
                var authEnabled = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_AUTH_SERVICE") ?? "false");
                var authenAccess = _serviceProvider.GetService<Sic.Login.AuthenAccess>();

                if (!authEnabled)
                {
                    componentHealth.IsHealthy = true;
                    componentHealth.Status = "Disabled";
                    componentHealth.Message = "Authentication is disabled in development mode";
                    componentHealth.Details = new Dictionary<string, object>
                    {
                        { "AuthenticationEnabled", false },
                        { "ServiceAvailable", authenAccess != null },
                        { "Mode", "Development" }
                    };
                    return componentHealth;
                }

                var authSettings = new[] { "JWT_SECRET", "ORGANIZATION_DOMAIN" };
                var configuredSettings = authSettings.Count(setting => 
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(setting)));

                componentHealth.IsHealthy = configuredSettings >= authSettings.Length && authenAccess != null;
                componentHealth.Status = componentHealth.IsHealthy ? "OK" : "Warning";
                componentHealth.Details = new Dictionary<string, object>
                {
                    { "AuthenticationEnabled", authEnabled },
                    { "ServiceAvailable", authenAccess != null },
                    { "ConfiguredSettings", configuredSettings },
                    { "TotalSettings", authSettings.Length },
                    { "RequiredSettings", authSettings }
                };

                if (!componentHealth.IsHealthy)
                {
                    var issues = new List<string>();
                    if (authenAccess == null) issues.Add("AuthenAccess service not available");
                    if (configuredSettings < authSettings.Length) issues.Add("Some authentication settings are missing");
                    componentHealth.Message = string.Join(", ", issues);
                }
            }
            catch (Exception ex)
            {
                componentHealth.IsHealthy = false;
                componentHealth.Status = "Error";
                componentHealth.Message = ex.Message;
            }

            return componentHealth;
        }

        private async Task<ComponentHealth> CheckServicesHealth()
        {
            var componentHealth = new ComponentHealth
            {
                Name = "Application Services",
                Type = "Services"
            };

            await Task.Delay(1);

            try
            {
                var services = new Dictionary<string, bool>
                {
                    { "DataService", IsServiceAvailable<IDataService>() },
                    { "ValidationService", IsServiceAvailable<IValidationService>() },
                    { "SharePointService", IsServiceAvailable<ISharePointService>() },
                    { "AuthenAccess", IsServiceAvailable<Sic.Login.AuthenAccess>() }
                };

                var availableServices = services.Count(s => s.Value);
                var totalServices = services.Count;

                var featureFlags = GetFeatureFlags();

                componentHealth.IsHealthy = true;
                componentHealth.Status = "OK";
                componentHealth.Details = new Dictionary<string, object>
                {
                    { "AvailableServices", availableServices },
                    { "TotalServices", totalServices },
                    { "ServiceStatus", services },
                    { "FeatureFlags", featureFlags },
                    { "MemoryUsage", GetMemoryUsage() }
                };
            }
            catch (Exception ex)
            {
                componentHealth.IsHealthy = false;
                componentHealth.Status = "Error";
                componentHealth.Message = ex.Message;
            }

            return componentHealth;
        }

        private async Task<ComponentHealth> CheckSharePointHealth()
        {
            var componentHealth = new ComponentHealth
            {
                Name = "SharePoint Integration",
                Type = "External Service"
            };

            var sharePointService = _serviceProvider.GetService<ISharePointService>();

            try
            {
                var sharePointEnabled = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_SHAREPOINT_SERVICE") ?? "false");

                if (!sharePointEnabled)
                {
                    return CreateDisabledComponentHealth(componentHealth, "SharePoint integration is disabled", sharePointService);
                }

                if (sharePointService == null)
                {
                    return CreateMissingServiceHealth(componentHealth, "SharePoint service is enabled but not available");
                }

                var connectionTestResult = await sharePointService.TestConnectionAsync("");

                if (connectionTestResult.Success)
                {
                    return await CreateSuccessfulSharePointHealth(componentHealth, sharePointService, connectionTestResult);
                }
                else
                {
                    return CreateFailedSharePointHealth(componentHealth, connectionTestResult);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return CreateUnauthorizedSharePointHealth(componentHealth, sharePointService);
            }
            catch (HttpRequestException ex)
            {
                return CreateHttpErrorSharePointHealth(componentHealth, sharePointService, ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                return CreateTimeoutSharePointHealth(componentHealth, sharePointService);
            }
            catch (Exception ex)
            {
                return CreateGeneralErrorSharePointHealth(componentHealth, sharePointService, ex);
            }
        }

        private ComponentHealth CheckPerformanceHealth()
        {
            var componentHealth = new ComponentHealth
            {
                Name = "Performance",
                Type = "System"
            };

            try
            {
                var process = Process.GetCurrentProcess();
                var workingSetMB = process.WorkingSet64 / 1024 / 1024;
                var isHealthy = workingSetMB < 500;

                componentHealth.IsHealthy = isHealthy;
                componentHealth.Status = isHealthy ? "OK" : "Warning";
                componentHealth.Details = new Dictionary<string, object>
                {
                    { "WorkingSetMB", workingSetMB },
                    { "ThreadCount", process.Threads.Count },
                    { "HandleCount", process.HandleCount },
                    { "StartTime", process.StartTime },
                    { "TotalProcessorTime", process.TotalProcessorTime.TotalMilliseconds },
                    { "MemoryThreshold", 500 }
                };

                if (!isHealthy)
                {
                    componentHealth.Message = $"High memory usage: {workingSetMB}MB (threshold: 500MB)";
                }
            }
            catch (Exception ex)
            {
                componentHealth.IsHealthy = false;
                componentHealth.Status = "Error";
                componentHealth.Message = ex.Message;
            }

            return componentHealth;
        }

        private async Task<SystemStatistics> GetSystemStatistics()
        {
            try
            {
                if (_dataService != null)
                {
                    var statistics = await _dataService.GetDataStatisticsAsync();
                    return new SystemStatistics
                    {
                        TotalRecords = statistics.TotalRecords,
                        RecentlyModified = statistics.RecentlyModifiedCount,
                        AverageSimilarity = statistics.AverageSimilarityScore,
                        DataCompleteness = statistics.DataCompletenessAverage,
                        MatchTypeBreakdown = statistics.MatchTypeBreakdown,
                        LastCalculated = statistics.LastCalculated
                    };
                }
                else
                {
                    return new SystemStatistics
                    {
                        TotalRecords = 0,
                        RecentlyModified = 0,
                        AverageSimilarity = 0,
                        DataCompleteness = 0,
                        MatchTypeBreakdown = new Dictionary<string, int>(),
                        LastCalculated = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get system statistics");
                return new SystemStatistics();
            }
        }

        private async Task<ComponentHealth> CheckSharePointHealthDetailed(string? userToken = null)
        {
            var componentHealth = new ComponentHealth
            {
                Name = "SharePoint Integration (Detailed)",
                Type = "External Service"
            };

            try
            {
                var sharePointService = _serviceProvider.GetService<ISharePointService>();
                var sharePointEnabled = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_SHAREPOINT_SERVICE") ?? "false");

                if (!sharePointEnabled || sharePointService == null)
                {
                    return await CheckSharePointHealth();
                }

                var healthDetails = new Dictionary<string, object>
                {
                    { "ServiceEnabled", true },
                    { "ServiceAvailable", true },
                    { "TestStartTime", DateTime.UtcNow }
                };

                var connectionTest = await sharePointService.TestConnectionAsync(userToken ?? "");
                healthDetails.Add("ConnectionTest", new
                {
                    Success = connectionTest.Success,
                    ResponseTime = connectionTest.ResponseTimeMs,
                    Message = connectionTest.Message
                });

                if (!string.IsNullOrEmpty(userToken))
                {
                    await AddDetailedSharePointTests(sharePointService, userToken, healthDetails);
                }

                var allTestsPassed = CheckAllTestsPassed(healthDetails);

                componentHealth.IsHealthy = allTestsPassed;
                componentHealth.Status = allTestsPassed ? "OK" : "Partial";
                componentHealth.Message = allTestsPassed
                    ? "All SharePoint tests passed"
                    : "Some SharePoint tests failed";
                componentHealth.Details = healthDetails;
            }
            catch (Exception ex)
            {
                componentHealth.IsHealthy = false;
                componentHealth.Status = "Error";
                componentHealth.Message = ex.Message;
                componentHealth.Details = new Dictionary<string, object>
                {
                    { "Error", ex.Message },
                    { "ErrorType", ex.GetType().Name }
                };
            }

            return componentHealth;
        }

        private ComponentHealth CheckSharePointConfiguration()
        {
            var componentHealth = new ComponentHealth
            {
                Name = "SharePoint Configuration",
                Type = "Configuration"
            };

            try
            {
                var requiredSettings = new Dictionary<string, string>
                {
                    { "SHAREPOINT_BASE_URL", Environment.GetEnvironmentVariable("SHAREPOINT_BASE_URL") ?? "" },
                    { "SHAREPOINT_SITE_URL", Environment.GetEnvironmentVariable("SHAREPOINT_SITE_URL") ?? "" },
                    { "OPPORTUNITY_LIST_TITLE", Environment.GetEnvironmentVariable("OPPORTUNITY_LIST_TITLE") ?? "" }
                };

                var configuredCount = requiredSettings.Count(s => !string.IsNullOrEmpty(s.Value));
                var totalRequired = requiredSettings.Count;

                componentHealth.IsHealthy = configuredCount == totalRequired;
                componentHealth.Status = configuredCount == totalRequired ? "OK" : "Incomplete";
                componentHealth.Message = $"SharePoint configuration: {configuredCount}/{totalRequired} settings configured";

                componentHealth.Details = new Dictionary<string, object>
                {
                    { "ConfiguredSettings", configuredCount },
                    { "RequiredSettings", totalRequired },
                    { "ConfigurationComplete", configuredCount == totalRequired }
                };

                foreach (var setting in requiredSettings)
                {
                    componentHealth.Details.Add($"Has_{setting.Key}", !string.IsNullOrEmpty(setting.Value));
                }
            }
            catch (Exception ex)
            {
                componentHealth.IsHealthy = false;
                componentHealth.Status = "Error";
                componentHealth.Message = ex.Message;
                componentHealth.Details = new Dictionary<string, object>
                {
                    { "Error", ex.Message }
                };
            }

            return componentHealth;
        }

        private async Task<SystemInformation> GetSystemInformation()
        {
            await Task.Delay(1);

            return new SystemInformation
            {
                Version = GetVersion(),
                Environment = GetEnvironment(),
                BuildDate = GetBuildDate(),
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                OSVersion = Environment.OSVersion.ToString(),
                RuntimeVersion = Environment.Version.ToString(),
                WorkingDirectory = Environment.CurrentDirectory,
                Configuration = GetSystemConfiguration()
            };
        }

        // Additional helper methods for SharePoint health checks
        private ComponentHealth CreateDisabledComponentHealth(ComponentHealth componentHealth, string message, ISharePointService? sharePointService)
        {
            componentHealth.IsHealthy = true;
            componentHealth.Status = "Disabled";
            componentHealth.Message = message;
            componentHealth.Details = new Dictionary<string, object>
            {
                { "ServiceEnabled", false },
                { "ServiceAvailable", sharePointService != null }
            };
            return componentHealth;
        }

        private ComponentHealth CreateMissingServiceHealth(ComponentHealth componentHealth, string message)
        {
            componentHealth.IsHealthy = false;
            componentHealth.Status = "Error";
            componentHealth.Message = message;
            componentHealth.Details = new Dictionary<string, object>
            {
                { "ServiceEnabled", true },
                { "ServiceAvailable", false },
                { "ErrorReason", "Service not registered in DI container" }
            };
            return componentHealth;
        }

        private async Task<ComponentHealth> CreateSuccessfulSharePointHealth(ComponentHealth componentHealth, ISharePointService sharePointService, dynamic connectionTestResult)
        {
            var listsResult = await sharePointService.GetAvailableListsAsync("");

            componentHealth.IsHealthy = true;
            componentHealth.Status = "OK";
            componentHealth.Message = "SharePoint service is healthy and responsive";
            componentHealth.Details = new Dictionary<string, object>
            {
                { "ServiceEnabled", true },
                { "ServiceAvailable", true },
                { "ConnectionStatus", "Connected" },
                { "ResponseTime", connectionTestResult.ResponseTimeMs },
                { "RequiresUserAuthentication", !listsResult.Success },
                { "LastChecked", DateTime.UtcNow }
            };

            if (listsResult.Success && listsResult.Data != null)
            {
                componentHealth.Details.Add("ListsFound", listsResult.Data.Count);
                componentHealth.Details.Add("ListsAccessible", true);
            }
            else
            {
                componentHealth.Details.Add("ListsAccessible", false);
                componentHealth.Details.Add("ListsErrorCode", listsResult.ErrorCode ?? "UNKNOWN");
            }

            return componentHealth;
        }

        private ComponentHealth CreateFailedSharePointHealth(ComponentHealth componentHealth, dynamic connectionTestResult)
        {
            componentHealth.IsHealthy = false;
            componentHealth.Status = "Error";
            componentHealth.Message = connectionTestResult.Message;
            componentHealth.Details = new Dictionary<string, object>
            {
                { "ServiceEnabled", true },
                { "ServiceAvailable", true },
                { "ConnectionStatus", "Failed" },
                { "ResponseTime", connectionTestResult.ResponseTimeMs },
                { "Error", connectionTestResult.Message },
                { "LastChecked", DateTime.UtcNow }
            };

            if (connectionTestResult.ConnectionDetails.Any())
            {
                foreach (var detail in connectionTestResult.ConnectionDetails)
                {
                    componentHealth.Details.TryAdd($"Connection_{detail.Key}", detail.Value);
                }
            }

            return componentHealth;
        }

        private ComponentHealth CreateUnauthorizedSharePointHealth(ComponentHealth componentHealth, ISharePointService? sharePointService)
        {
            componentHealth.IsHealthy = true;
            componentHealth.Status = "RequiresAuth";
            componentHealth.Message = "SharePoint service is healthy but requires user authentication";
            componentHealth.Details = new Dictionary<string, object>
            {
                { "ServiceEnabled", true },
                { "ServiceAvailable", sharePointService != null },
                { "ConnectionStatus", "RequiresAuthentication" },
                { "AuthenticationRequired", true },
                { "Message", "Service is working but needs user token for full functionality" }
            };
            return componentHealth;
        }

        private ComponentHealth CreateHttpErrorSharePointHealth(ComponentHealth componentHealth, ISharePointService? sharePointService, HttpRequestException ex)
        {
            componentHealth.IsHealthy = false;
            componentHealth.Status = "Error";
            componentHealth.Message = "SharePoint HTTP connection failed";
            componentHealth.Details = new Dictionary<string, object>
            {
                { "ServiceEnabled", true },
                { "ServiceAvailable", sharePointService != null },
                { "ConnectionStatus", "Failed" },
                { "Error", ex.Message },
                { "ErrorType", "HTTP" },
                { "StatusCode", ex.Data.Contains("StatusCode") ? ex.Data["StatusCode"]?.ToString() ?? "Unknown" : "Unknown" }
            };
            return componentHealth;
        }

        private ComponentHealth CreateTimeoutSharePointHealth(ComponentHealth componentHealth, ISharePointService? sharePointService)
        {
            componentHealth.IsHealthy = false;
            componentHealth.Status = "Timeout";
            componentHealth.Message = "SharePoint connection timed out";
            componentHealth.Details = new Dictionary<string, object>
            {
                { "ServiceEnabled", true },
                { "ServiceAvailable", sharePointService != null },
                { "ConnectionStatus", "Timeout" },
                { "Error", "Connection request timed out" }
            };
            return componentHealth;
        }

        private ComponentHealth CreateGeneralErrorSharePointHealth(ComponentHealth componentHealth, ISharePointService? sharePointService, Exception ex)
        {
            componentHealth.IsHealthy = false;
            componentHealth.Status = "Error";
            componentHealth.Message = $"SharePoint health check failed: {ex.Message}";
            componentHealth.Details = new Dictionary<string, object>
            {
                { "ServiceEnabled", true },
                { "ServiceAvailable", sharePointService != null },
                { "ConnectionStatus", "Failed" },
                { "Error", ex.Message },
                { "ErrorType", ex.GetType().Name }
            };
            return componentHealth;
        }

        private async Task AddDetailedSharePointTests(ISharePointService sharePointService, string userToken, Dictionary<string, object> healthDetails)
        {
            var userDiagnostic = await sharePointService.DiagnoseUserAsync(userToken);
            healthDetails.Add("UserDiagnostic", new
            {
                Success = userDiagnostic.Success,
                HasToken = userDiagnostic.HasToken,
                UserInfo = userDiagnostic.UserInfo?.DisplayName ?? "Unknown",
                CanAccessSites = userDiagnostic.SharePointAccess?.CanAccessSites ?? false
            });

            var permissionTest = await sharePointService.ValidateUserPermissionsAsync(userToken);
            healthDetails.Add("PermissionValidation", new
            {
                Success = permissionTest.Success,
                HasAccess = permissionTest.HasAccess,
                PermissionsCount = permissionTest.Permissions.Count,
                ListsCount = permissionTest.Lists.Count
            });

            var listsTest = await sharePointService.GetAvailableListsAsync(userToken);
            healthDetails.Add("ListsTest", new
            {
                Success = listsTest.Success,
                ListsCount = listsTest.Data?.Count ?? 0,
                ErrorCode = listsTest.ErrorCode
            });

            var opportunitiesTest = await sharePointService.GetOpportunityListAsync(userToken);
            healthDetails.Add("OpportunitiesTest", new
            {
                Success = opportunitiesTest.Success,
                ContactsCount = opportunitiesTest.Data?.Count ?? 0,
                ErrorCode = opportunitiesTest.ErrorCode
            });
        }

        private bool CheckAllTestsPassed(Dictionary<string, object> healthDetails)
        {
            return healthDetails.Values
                .OfType<object>()
                .Where(v => v.GetType().GetProperty("Success") != null)
                .All(v => (bool)(v.GetType().GetProperty("Success")?.GetValue(v) ?? false));
        }

        private Dictionary<string, bool> GetFeatureFlags()
        {
            return new Dictionary<string, bool>
            {
                { "DataService", bool.Parse(Environment.GetEnvironmentVariable("ENABLE_DATA_SERVICE") ?? "true") },
                { "MatchingService", bool.Parse(Environment.GetEnvironmentVariable("ENABLE_MATCHING_SERVICE") ?? "false") },
                { "SharePointService", bool.Parse(Environment.GetEnvironmentVariable("ENABLE_SHAREPOINT_SERVICE") ?? "false") },
                { "AuthService", bool.Parse(Environment.GetEnvironmentVariable("ENABLE_AUTH_SERVICE") ?? "false") }
            };
        }

        private long GetMemoryUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return process.WorkingSet64 / 1024 / 1024;
            }
            catch
            {
                return 0;
            }
        }

        private string GetVersion() => Environment.GetEnvironmentVariable("API_VERSION") ?? "1.0.0";
        private string GetEnvironment() => Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Unknown";
        private DateTime GetBuildDate() => System.IO.File.GetCreationTime(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private Dictionary<string, object> GetSystemConfiguration()
        {
            return new Dictionary<string, object>
            {
                // Database Configuration
                { "TableName", Environment.GetEnvironmentVariable("TableName") ?? "Not Set" },
                { "SourceTableName", Environment.GetEnvironmentVariable("SourceTableName") ?? "Not Set" },
                { "MatchHistoryTableName", Environment.GetEnvironmentVariable("MatchHistoryTableName") ?? "Not Set" },
                
                // Matching Configuration
                { "SimilarityThresholdAuto", Environment.GetEnvironmentVariable("SIMILARITY_THRESHOLD_AUTO") ?? "80" },
                { "SimilarityThresholdSuggest", Environment.GetEnvironmentVariable("SIMILARITY_THRESHOLD_SUGGEST") ?? "60" },
                { "MaxSuggestionsPerRecord", Environment.GetEnvironmentVariable("MAX_SUGGESTIONS_PER_RECORD") ?? "5" },
                
                // Feature Flags
                { "EnableDataValidation", Environment.GetEnvironmentVariable("ENABLE_DATA_VALIDATION") ?? "true" },
                { "EnableDataService", Environment.GetEnvironmentVariable("ENABLE_DATA_SERVICE") ?? "true" },
                { "EnableAuthService", Environment.GetEnvironmentVariable("ENABLE_AUTH_SERVICE") ?? "false" },
                { "EnableSharePointService", Environment.GetEnvironmentVariable("ENABLE_SHAREPOINT_SERVICE") ?? "false" },
                { "EnableMatchingService", Environment.GetEnvironmentVariable("ENABLE_MATCHING_SERVICE") ?? "false" },
                
                // Performance Configuration
                { "BatchProcessingSize", Environment.GetEnvironmentVariable("BATCH_PROCESSING_SIZE") ?? "50" },
                { "MaxConcurrentRequests", Environment.GetEnvironmentVariable("MAX_CONCURRENT_REQUESTS") ?? "5" }
            };
        }
    }

    // Health check models (ไม่เปลี่ยนแปลง)
    public class HealthStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = "";

        [JsonPropertyName("isHealthy")]
        public bool IsHealthy { get; set; } = true;

        [JsonPropertyName("components")]
        public List<ComponentHealth> Components { get; set; } = new();
    }

    public class DetailedHealthStatus : HealthStatus
    {
        [JsonPropertyName("statistics")]
        public SystemStatistics Statistics { get; set; } = new();
    }

    public class ComponentHealth
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("isHealthy")]
        public bool IsHealthy { get; set; } = true;

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("responseTime")]
        public TimeSpan ResponseTime { get; set; }

        [JsonPropertyName("details")]
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class SystemStatistics
    {
        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; set; } = 0;

        [JsonPropertyName("recentlyModified")]
        public int RecentlyModified { get; set; } = 0;

        [JsonPropertyName("averageSimilarity")]
        public double AverageSimilarity { get; set; } = 0;

        [JsonPropertyName("dataCompleteness")]
        public double DataCompleteness { get; set; } = 0;

        [JsonPropertyName("matchTypeBreakdown")]
        public Dictionary<string, int> MatchTypeBreakdown { get; set; } = new();

        [JsonPropertyName("lastCalculated")]
        public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
    }

    public class SystemInformation
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = "";

        [JsonPropertyName("buildDate")]
        public DateTime BuildDate { get; set; }

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; } = "";

        [JsonPropertyName("processorCount")]
        public int ProcessorCount { get; set; } = 0;

        [JsonPropertyName("osVersion")]
        public string OSVersion { get; set; } = "";

        [JsonPropertyName("runtimeVersion")]
        public string RuntimeVersion { get; set; } = "";

        [JsonPropertyName("workingDirectory")]
        public string WorkingDirectory { get; set; } = "";

        [JsonPropertyName("configuration")]
        public Dictionary<string, object> Configuration { get; set; } = new();
    }
}