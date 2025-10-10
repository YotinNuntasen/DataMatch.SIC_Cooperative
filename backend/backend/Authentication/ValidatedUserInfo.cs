using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace DataMatchBackend.Authentication;

/// <summary>
/// Validated user information extracted from authentication token
/// </summary>
public class ValidatedUserInfo
{
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; 
    
    [JsonPropertyName("objectId")]
    public string ObjectId { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("givenName")]
    public string GivenName { get; set; } = string.Empty;
    
    [JsonPropertyName("familyName")]
    public string FamilyName { get; set; } = string.Empty;
    
    [JsonPropertyName("displayName")]
    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : $"{GivenName} {FamilyName}".Trim();
    
    [JsonPropertyName("preferredUsername")]
    public string PreferredUsername { get; set; } = string.Empty;

    // Job Information
    [JsonPropertyName("jobTitle")]
    public string JobTitle { get; set; } = string.Empty;
    
    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    // Authorization
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
    
    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();

    // Authentication Context
    [JsonPropertyName("isAuthenticated")]
    public bool IsAuthenticated { get; set; } = false;
    
    [JsonPropertyName("authenticationType")]
    public string AuthenticationType { get; set; } = "JWT";
    
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;
    
    [JsonPropertyName("audience")]
    public string Audience { get; set; } = string.Empty;
    
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    // Session Information
    [JsonPropertyName("loginTime")]
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("tokenExpiresAt")]
    public DateTime? TokenExpiresAt { get; set; }
    
    [JsonPropertyName("lastActivity")]
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    // Security Context (not serialized for security)
    [JsonIgnore]
    public SecurityToken? SecurityToken { get; set; }
    
    [JsonIgnore]
    public ClaimsPrincipal? ClaimsPrincipal { get; set; }

    // Helper methods
    public bool HasRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasAnyRole(params string[] roles)
    {
        return roles.Any(role => Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsTokenExpired()
    {
        return TokenExpiresAt.HasValue && TokenExpiresAt.Value <= DateTime.UtcNow;
    }

    public bool IsSessionActive(TimeSpan maxInactivity)
    {
        return DateTime.UtcNow.Subtract(LastActivity) <= maxInactivity;
    }

    public void UpdateLastActivity()
    {
        LastActivity = DateTime.UtcNow;
    }

    public string GetInitials()
    {
        var initials = "";
        if (!string.IsNullOrEmpty(GivenName))
            initials += GivenName[0];
        if (!string.IsNullOrEmpty(FamilyName))
            initials += FamilyName[0];
        
        return initials.ToUpperInvariant();
    }

    public Dictionary<string, object> ToClaimsDictionary()
    {
        return new Dictionary<string, object>
        {
            { "userId", UserId },
            { "email", Email },
            { "name", Name },
            { "givenName", GivenName },
            { "familyName", FamilyName },
            { "jobTitle", JobTitle },
            { "department", Department },
            { "roles", string.Join(",", Roles) },
            { "permissions", string.Join(",", Permissions) },
            { "tenantId", TenantId },
            { "objectId", ObjectId },
            { "preferredUsername", PreferredUsername },
            { "sessionId", SessionId }
        };
    }

    public override string ToString()
    {
        return $"{DisplayName} ({Email}) - {string.Join(", ", Roles)}";
    }
}