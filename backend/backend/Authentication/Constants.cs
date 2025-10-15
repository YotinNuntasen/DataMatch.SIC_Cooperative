using System;
using Environment = System.Environment;

namespace Sic.Login;

/// <summary>
/// Authentication constants for Silicon Craft Data Match Portal
/// </summary>
internal static class Constants
{
    #region Azure AD B2C Configuration

    internal static string Audience => Environment.GetEnvironmentVariable("JWT_AUDIENCE") ??
                                     "https://siliconcraft.onmicrosoft.com/data-match-api";

    internal static string ClientId => Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID") ??
                                     "your-backend-client-id";

    internal static string Tenant => Environment.GetEnvironmentVariable("AD_TENANT") ??
                                   "siliconcraft.onmicrosoft.com";

    internal static string TenantId => Environment.GetEnvironmentVariable("GRAPH_TENANT_ID") ??
                                     "your-tenant-id-guid";

    internal static string ClientSecret => Environment.GetEnvironmentVariable("GRAPH_CLIENT_SECRET") ??
                                         "your-client-secret";

    internal static string Authority => Environment.GetEnvironmentVariable("JWT_AUTHORITY") ??
                                      $"https://login.microsoftonline.com/{TenantId}";

    #endregion

    #region Token Validation

    internal static string[] ValidIssuers => new[]
    {
        $"https://sts.windows.net/{TenantId}/",
        $"https://login.microsoftonline.com/{TenantId}/v2.0",
        $"https://login.microsoftonline.com/{TenantId}/",
        Authority
    };

    internal static string[] scopes => new[]
    {
        Environment.GetEnvironmentVariable("GRAPH_SCOPES") ?? "https://sicth.sharepoint.com/.default"
    };

    #endregion

    #region Application Roles

    internal static class Roles
    {
        internal const string Admin = "DataMatch.Admin";
        internal const string Manager = "DataMatch.Manager";
        internal const string User = "DataMatch.User";
        internal const string Viewer = "DataMatch.Viewer";
        internal const string System = "DataMatch.System";
    }

    #endregion

    #region Permissions

    internal static class Permissions
    {
        internal const string DataRead = "data.read";
        internal const string DataWrite = "data.write";
        internal const string DataDelete = "data.delete";
        internal const string DataExport = "data.export";
        internal const string DataImport = "data.import";
        internal const string MatchingView = "matching.view";
        internal const string MatchingCreate = "matching.create";
        internal const string MatchingEdit = "matching.edit";
        internal const string MatchingDelete = "matching.delete";
        internal const string MatchingAuto = "matching.auto";
        internal const string SharePointRead = "sharepoint.read";
        internal const string SharePointWrite = "sharepoint.write";
        internal const string SharePointConfig = "sharepoint.config";
        internal const string SystemConfig = "system.config";
        internal const string SystemMonitor = "system.monitor";
        internal const string SystemAdmin = "system.admin";
        internal const string UserView = "user.view";
        internal const string UserManage = "user.manage";
        internal const string RoleAssign = "role.assign";
        internal const string AnalyticsView = "analytics.view";
        internal const string AnalyticsExport = "analytics.export";
        internal const string ReportsGenerate = "reports.generate";
    }

    #endregion

    #region Organization Configuration

    internal static class Organization
    {
        internal static string Domain => Environment.GetEnvironmentVariable("ORGANIZATION_DOMAIN") ?? "siliconcraft.com";
        internal static string Name => Environment.GetEnvironmentVariable("ORGANIZATION_NAME") ?? "Silicon Craft Technology";
        internal static string Country => Environment.GetEnvironmentVariable("ORGANIZATION_COUNTRY") ?? "Thailand";

        internal static string[] AllowedDomains =>
            Environment.GetEnvironmentVariable("ALLOWED_DOMAINS")?.Split(',') ?? new[] { 
                "siliconcraft.com", 
                "SiliconCraftTechnologyB2C.onmicrosoft.com" 
            };
    }

    #endregion

    #region Helper Methods

    internal static List<string> GetRolesForUser(string? department, string? jobTitle)
    {
        var roles = new List<string> { Roles.User };

        if (!string.IsNullOrEmpty(department))
        {
            switch (department.ToLowerInvariant())
            {
                case "it":
                case "information technology":
                    roles.Add(Roles.Admin);
                    break;
                case "management":
                    roles.Add(Roles.Manager);
                    break;
            }
        }

        if (!string.IsNullOrEmpty(jobTitle))
        {
            var title = jobTitle.ToLowerInvariant();
            if (title.Contains("manager") || title.Contains("director"))
            {
                roles.Add(Roles.Manager);
            }
            if (title.Contains("cto") || title.Contains("admin"))
            {
                roles.Add(Roles.Admin);
            }
        }

        return roles.Distinct().ToList();
    }

    internal static List<string> GetEffectivePermissions(IEnumerable<string> userRoles)
    {
        var permissions = new List<string> { Permissions.DataRead, Permissions.MatchingView };

        foreach (var role in userRoles)
        {
            switch (role)
            {
                case Roles.Admin:
                    permissions.AddRange(new[]
                    {
                        Permissions.DataWrite, Permissions.DataDelete, Permissions.DataExport,
                        Permissions.MatchingCreate, Permissions.MatchingEdit, Permissions.MatchingDelete,
                        Permissions.SystemConfig, Permissions.SystemAdmin, Permissions.UserManage
                    });
                    break;
                case Roles.Manager:
                    permissions.AddRange(new[]
                    {
                        Permissions.DataWrite, Permissions.DataExport,
                        Permissions.MatchingCreate, Permissions.MatchingEdit,
                        Permissions.UserView
                    });
                    break;
                case Roles.User:
                    permissions.AddRange(new[]
                    {
                        Permissions.DataWrite, Permissions.MatchingCreate
                    });
                    break;
            }
        }

        return permissions.Distinct().ToList();
    }

    #endregion
}