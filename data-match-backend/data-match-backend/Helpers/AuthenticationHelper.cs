using Microsoft.Azure.Functions.Worker.Http;
using DataMatchBackend.Authentication;
using Sic.Login;
using Microsoft.Extensions.DependencyInjection;

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
    }
}