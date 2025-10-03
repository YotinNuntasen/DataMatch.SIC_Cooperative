using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using DataMatchBackend.Authentication;
using FunctionHttpRequestData = Microsoft.Azure.Functions.Worker.Http.HttpRequestData;

namespace Sic.Login;

public class AuthenAccess
{
    private readonly ILogger<AuthenAccess> _logger;

    public AuthenAccess(ILogger<AuthenAccess> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Main token validation for Azure Functions
    /// </summary>
    public async Task<ValidatedUserInfo?> ValidateTokenAsync(FunctionHttpRequestData req)
    {
        try
        {
            var accessToken = ExtractTokenFromRequest(req);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("No access token found in request");
                return null;
            }

            return await ValidateAndParseToken(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return null;
        }
    }

   

    /// <summary>
    /// Generate JWT token for user
    /// </summary>
    public string GenerateJwtToken(ValidatedUserInfo userInfo)
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            if (environment == "Development")
            {
                return GenerateSimpleToken(userInfo);
            }

            // TODO: Implement proper JWT generation for production
            return GenerateSimpleToken(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token");
            throw;
        }
    }

    // Private methods
    private string? ExtractTokenFromRequest(FunctionHttpRequestData req)
    {
        if (req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            var authHeader = authHeaders.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
        }
        return null;
    }

    private async Task<ValidatedUserInfo?> ValidateAndParseToken(string accessToken)
    {
        var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        if (environment == "Development")
        {
            return CreateDevelopmentUser();
        }

        // Production token validation
        return await ValidateProductionToken(accessToken);
    }

    private ValidatedUserInfo CreateDevelopmentUser()
    {
        var roles = Constants.GetRolesForUser("IT", "Developer");
        var permissions = Constants.GetEffectivePermissions(roles);

        return new ValidatedUserInfo
        {
            UserId = "dev-user-" + Guid.NewGuid().ToString("N")[..8],
            Email = "developer@siliconcraft.com",
            Name = "Development User",
            GivenName = "Development",
            FamilyName = "User",
            JobTitle = "Software Developer",
            Department = "IT",
            Roles = roles,
            Permissions = permissions,
            IsAuthenticated = true,
            TenantId = Constants.TenantId,
            LoginTime = DateTime.UtcNow,
            TokenExpiresAt = DateTime.UtcNow.AddHours(24),
            AuthenticationType = "Development"
        };
    }

    private async Task<ValidatedUserInfo?> ValidateProductionToken(string accessToken)
    {
        try
        {
            var audience = Constants.Audience;
            var clientId = Constants.ClientId;
            var authority = Constants.Authority;
            var validIssuers = Constants.ValidIssuers;

            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{authority}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());

            var config = await configManager.GetConfigurationAsync();
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidAudiences = new[] {
        audience,
        clientId,
        "00000003-0000-0ff1-ce00-000000000000"
    },
                ValidIssuers = validIssuers,
                IssuerSigningKeys = config.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            return MapClaimsToUserInfo(jwtToken.Claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Production token validation failed");
            return null;
        }
    }

    private ValidatedUserInfo MapClaimsToUserInfo(IEnumerable<Claim> claims)
    {
        var claimDict = claims.ToDictionary(c => c.Type, c => c.Value);

        var userInfo = new ValidatedUserInfo
        {
            UserId = GetClaimValue(claimDict, "sub") ?? GetClaimValue(claimDict, "oid") ?? "",
            Email = GetClaimValue(claimDict, "email") ?? GetClaimValue(claimDict, "preferred_username") ?? "",
            Name = GetClaimValue(claimDict, "name") ?? "",
            GivenName = GetClaimValue(claimDict, "given_name") ?? "",
            FamilyName = GetClaimValue(claimDict, "family_name") ?? "",
            JobTitle = GetClaimValue(claimDict, "jobTitle") ?? "",
            Department = GetClaimValue(claimDict, "department") ?? "",
            ObjectId = GetClaimValue(claimDict, "oid") ?? "",
            TenantId = Constants.TenantId,
            IsAuthenticated = true,
            AuthenticationType = "AzureAD",
            LoginTime = DateTime.UtcNow,
            SessionId = Guid.NewGuid().ToString()
        };

        // Set roles and permissions based on user attributes
        userInfo.Roles = Constants.GetRolesForUser(userInfo.Department, userInfo.JobTitle);
        userInfo.Permissions = Constants.GetEffectivePermissions(userInfo.Roles);

        return userInfo;
    }

    private string GenerateSimpleToken(ValidatedUserInfo userInfo)
    {
        var tokenData = new
        {
            userId = userInfo.UserId,
            email = userInfo.Email,
            name = userInfo.Name,
            roles = userInfo.Roles,
            issued = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            expires = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds()
        };

        var tokenJson = System.Text.Json.JsonSerializer.Serialize(tokenData);
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenJson);
        return Convert.ToBase64String(tokenBytes);
    }

    private static string? GetClaimValue(Dictionary<string, string> claims, string key)
    {
        return claims.TryGetValue(key, out var value) ? value : null;
    }
}