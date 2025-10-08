using Microsoft.Azure.Functions.Worker.Http;
using DataMatchBackend.Authentication;
using Sic.Login;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text;

namespace DataMatchBackend.Helpers
{
    public static class AuthenticationHelper
    {
        public static async Task<(bool IsValid, string? UserToken, string ErrorMessage)> ValidateConditionalAsync(
            HttpRequestData req, 
            IServiceProvider serviceProvider)
        {
            var enableAuth = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_AUTH_SERVICE") ?? "false");

            if (!enableAuth)
            {
                return (true, "dev-token", "");
            }

            // ✅ ลองใช้ Static Web Apps authentication ก่อน
            var staticWebAppsAuth = TryValidateStaticWebAppsAuth(req);
            if (staticWebAppsAuth.IsValid)
            {
                return staticWebAppsAuth;
            }

            // ✅ ถ้าไม่มี x-ms-client-principal ให้ใช้ Bearer token แบบเดิม
            var authenAccess = serviceProvider.GetService<AuthenAccess>();
            if (authenAccess == null)
            {
                return (false, null, "Authentication service not configured");
            }

            try
            {
                var userToken = ExtractUserToken(req);
                if (string.IsNullOrEmpty(userToken))
                {
                    return (false, null, "Authorization header missing");
                }

                var userInfo = await authenAccess.ValidateTokenAsync(req);
                return userInfo == null 
                    ? (false, null, "Authentication failed") 
                    : (true, userToken, "");
            }
            catch (Exception)
            {
                return (false, null, "Authentication error");
            }
        }

        // ✅ Method ใหม่: ตรวจสอบ Static Web Apps auth
        private static (bool IsValid, string? UserToken, string ErrorMessage) TryValidateStaticWebAppsAuth(HttpRequestData req)
        {
            try
            {
                // อ่าน x-ms-client-principal header
                var principalHeader = req.Headers
                    .FirstOrDefault(h => h.Key.Equals("x-ms-client-principal", StringComparison.OrdinalIgnoreCase));

                if (principalHeader.Value == null || !principalHeader.Value.Any())
                {
                    // ไม่มี header นี้ = ไม่ใช่ Static Web Apps auth
                    return (false, null, "");
                }

                var data = principalHeader.Value.First();
                if (string.IsNullOrWhiteSpace(data))
                {
                    return (false, null, "");
                }

                // Decode และ deserialize
                var decoded = Convert.FromBase64String(data);
                var json = Encoding.UTF8.GetString(decoded);
                var principal = JsonSerializer.Deserialize<ClientPrincipal>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (principal == null || string.IsNullOrEmpty(principal.UserId))
                {
                    return (false, null, "Invalid authentication principal");
                }

                // ✅ ตรวจสอบว่า user มี role "authenticated" หรือไม่
                var isAuthenticated = principal.UserRoles?.Contains("authenticated") == true;
                if (!isAuthenticated)
                {
                    return (false, null, "User not authenticated");
                }

                // ✅ Success! ใช้ UserId เป็น token
                return (true, principal.UserId, "");
            }
            catch (Exception ex)
            {
                // ถ้า decode ไม่สำเร็จ = ไม่ใช่ Static Web Apps auth
                return (false, null, $"Static Web Apps auth failed: {ex.Message}");
            }
        }

        public static string? ExtractUserToken(HttpRequestData req)
        {
            try
            {
                var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();
                return authHeader?.StartsWith("Bearer ") == true 
                    ? authHeader.Substring("Bearer ".Length).Trim() 
                    : null;
            }
            catch
            {
                return null;
            }
        }

        // ✅ Class สำหรับ deserialize Static Web Apps principal
        private class ClientPrincipal
        {
            public string IdentityProvider { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string UserDetails { get; set; } = string.Empty;
            public string[] UserRoles { get; set; } = Array.Empty<string>();
        }
    }
}