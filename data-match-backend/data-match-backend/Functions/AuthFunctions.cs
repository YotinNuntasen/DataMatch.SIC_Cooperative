using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DataMatchBackend.Authentication;
using DataMatchBackend.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sic.Login;

namespace DataMatchBackend.Functions
{
    public class AuthFunctions : BaseFunctionService
    {
        private readonly AuthenAccess? _authenAccess;

        public AuthFunctions(ILogger<AuthFunctions> logger, IServiceProvider serviceProvider) 
            : base(logger, serviceProvider)
        {
            _authenAccess = serviceProvider.GetService<AuthenAccess>();
        }

        [Function("ValidateToken")]
        public async Task<HttpResponseData> ValidateToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/validate")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Validating authentication token");

                // ตรวจสอบว่า Auth Service เปิดใช้งานไหม
                if (!IsServiceAvailable<AuthenAccess>())
                {
                    return await CreateServiceUnavailableResponse(req, "Authentication", "ENABLE_AUTH_SERVICE");
                }

                // เรียกใช้ AuthenAccess เพื่อตรวจสอบ token
                var userInfo = await _authenAccess!.ValidateTokenAsync(req);

                if (userInfo == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid or expired token");
                }

                _logger.LogInformation("Token validated successfully for user: {Email}", userInfo.Email);
                return await CreateOkResponse(req, userInfo, "Token validated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Token validation failed");
            }
        }

        [Function("GetCurrentUser")]
        public async Task<HttpResponseData> GetCurrentUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting current user information");

                if (!IsServiceAvailable<AuthenAccess>())
                {
                    return await CreateServiceUnavailableResponse(req, "Authentication", "ENABLE_AUTH_SERVICE");
                }

                var userInfo = await _authenAccess!.ValidateTokenAsync(req);
                if (userInfo == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                userInfo.UpdateLastActivity();
                _logger.LogInformation("Retrieved user information for: {Email}", userInfo.Email);
                
                return await CreateOkResponse(req, userInfo, "User information retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get user information");
            }
        }

        [Function("RefreshToken")]
        public async Task<HttpResponseData> RefreshToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/refresh")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Refreshing authentication token");

                if (!IsServiceAvailable<AuthenAccess>())
                {
                    return await CreateServiceUnavailableResponse(req, "Authentication", "ENABLE_AUTH_SERVICE");
                }

                var userInfo = await _authenAccess!.ValidateTokenAsync(req);
                if (userInfo == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid token for refresh");
                }

                var newToken = _authenAccess.GenerateJwtToken(userInfo);
                var expiresIn = Environment.GetEnvironmentVariable("JWT_EXPIRES_IN") ?? "24h";

                _logger.LogInformation("Token refreshed successfully for user: {Email}", userInfo.Email);

                var tokenData = new
                {
                    accessToken = newToken,
                    tokenType = "Bearer",
                    expiresIn = expiresIn,
                    user = userInfo
                };

                return await CreateOkResponse(req, tokenData, "Token refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Token refresh failed");
            }
        }

        [Function("CheckPermissions")]
        public async Task<HttpResponseData> CheckPermissions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/permissions")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Checking user permissions");

                if (!IsServiceAvailable<AuthenAccess>())
                {
                    return await CreateServiceUnavailableResponse(req, "Authentication", "ENABLE_AUTH_SERVICE");
                }

                var userInfo = await _authenAccess!.ValidateTokenAsync(req);
                if (userInfo == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                var requestBody = await req.ReadAsStringAsync();
                var permissionRequest = JsonSerializer.Deserialize<PermissionCheckRequest>(requestBody ?? "{}");

                var (hasPermissions, missingPermissions) = CheckUserPermissions(userInfo, permissionRequest);

                var statusCode = hasPermissions ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
                var permissionData = new
                {
                    hasPermissions,
                    userRoles = userInfo.Roles,
                    userPermissions = userInfo.Permissions,
                    missingPermissions = missingPermissions
                };

                var response = req.CreateResponse(statusCode);
                await response.WriteAsJsonAsync(new ApiResponse<object>
                {
                    Code = (int)statusCode,
                    Message = hasPermissions ? "Permissions granted" : "Insufficient permissions",
                    Data = permissionData,
                    Success = hasPermissions
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permissions");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Permission check failed");
            }
        }

        [Function("GenerateTestToken")]
        public async Task<HttpResponseData> GenerateTestToken(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "auth/test-token")] HttpRequestData req)
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
                if (environment != "Development")
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Forbidden, 
                        "Test token generation is only available in development environment");
                }

                _logger.LogInformation("Generating test token for development");

                if (!IsServiceAvailable<AuthenAccess>())
                {
                    return await CreateServiceUnavailableResponse(req, "Authentication", "ENABLE_AUTH_SERVICE");
                }

                var requestBody = await req.ReadAsStringAsync();
                var testUserRequest = JsonSerializer.Deserialize<TestUserRequest>(requestBody ?? "{}");

                var testUser = CreateTestUser(testUserRequest);
                var token = _authenAccess!.GenerateJwtToken(testUser);

                _logger.LogInformation("Test token generated for user: {Email}", testUser.Email);

                var tokenData = new
                {
                    accessToken = token,
                    tokenType = "Bearer",
                    expiresIn = Environment.GetEnvironmentVariable("JWT_EXPIRES_IN") ?? "24h",
                    user = testUser
                };

                return await CreateOkResponse(req, tokenData, "Test token generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test token");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Test token generation failed");
            }
        }

        [Function("Logout")]
        public async Task<HttpResponseData> Logout(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Processing logout request");

                if (_authenAccess != null)
                {
                    var userInfo = await _authenAccess.ValidateTokenAsync(req);
                    if (userInfo != null)
                    {
                        _logger.LogInformation("User logged out: {Email}", userInfo.Email);
                    }
                }
                else
                {
                    _logger.LogInformation("Logout processed (auth service disabled)");
                }

                var logoutData = new
                {
                    loggedOut = true,
                    timestamp = DateTime.UtcNow,
                    authServiceEnabled = _authenAccess != null
                };

                return await CreateOkResponse(req, logoutData, "Logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing logout");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Logout failed");
            }
        }

        [Function("GetUserActivity")]
        public async Task<HttpResponseData> GetUserActivity(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/activity")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting user activity");

                if (!IsServiceAvailable<AuthenAccess>())
                {
                    return await CreateServiceUnavailableResponse(req, "Authentication", "ENABLE_AUTH_SERVICE");
                }

                var userInfo = await _authenAccess!.ValidateTokenAsync(req);
                if (userInfo == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                var activity = new
                {
                    userId = userInfo.UserId,
                    email = userInfo.Email,
                    loginTime = userInfo.LoginTime,
                    lastActivity = userInfo.LastActivity,
                    sessionDuration = DateTime.UtcNow.Subtract(userInfo.LoginTime).TotalMinutes,
                    isActive = !userInfo.IsTokenExpired(),
                    sessionId = userInfo.SessionId
                };

                return await CreateOkResponse(req, activity, "Activity retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activity");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get activity");
            }
        }

        // Private helper methods
        private (bool hasPermissions, List<string> missingPermissions) CheckUserPermissions(
            ValidatedUserInfo userInfo, 
            PermissionCheckRequest? permissionRequest)
        {
            var hasPermissions = true;
            var missingPermissions = new List<string>();

            if (permissionRequest?.RequiredRoles != null)
            {
                foreach (var role in permissionRequest.RequiredRoles)
                {
                    if (!userInfo.HasRole(role))
                    {
                        hasPermissions = false;
                        missingPermissions.Add($"Role: {role}");
                    }
                }
            }

            if (permissionRequest?.RequiredPermissions != null)
            {
                foreach (var permission in permissionRequest.RequiredPermissions)
                {
                    if (!userInfo.HasPermission(permission))
                    {
                        hasPermissions = false;
                        missingPermissions.Add($"Permission: {permission}");
                    }
                }
            }

            return (hasPermissions, missingPermissions);
        }

        private ValidatedUserInfo CreateTestUser(TestUserRequest? testUserRequest)
        {
            return new ValidatedUserInfo
            {
                UserId = testUserRequest?.UserId ?? Guid.NewGuid().ToString(),
                Email = testUserRequest?.Email ?? "test@siliconcraft.com",
                Name = testUserRequest?.Name ?? "Test User",
                GivenName = testUserRequest?.GivenName ?? "Test",
                FamilyName = testUserRequest?.FamilyName ?? "User",
                JobTitle = testUserRequest?.JobTitle ?? "Developer",
                Department = testUserRequest?.Department ?? "IT",
                Roles = testUserRequest?.Roles ?? new List<string> { Constants.Roles.User },
                IsAuthenticated = true,
                TenantId = Constants.TenantId,
                LoginTime = DateTime.UtcNow
            };
        }
    }

    // Request models (ไม่เปลี่ยนแปลง)
    public class PermissionCheckRequest
    {
        [JsonPropertyName("requiredRoles")]
        public List<string>? RequiredRoles { get; set; }

        [JsonPropertyName("requiredPermissions")]
        public List<string>? RequiredPermissions { get; set; }
    }

    public class TestUserRequest
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("givenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("familyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("jobTitle")]
        public string? JobTitle { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }

        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }
    }
}