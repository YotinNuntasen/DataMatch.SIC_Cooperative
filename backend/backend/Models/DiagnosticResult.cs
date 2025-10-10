namespace DataMatchBackend.Models
{
    public class DiagnosticResult
    {
        public DateTime Timestamp { get; set; }
        public string? Environment { get; set; }
        public DiagnosticConfiguration Configurations { get; set; } = new();
        public SharePointAuthResult SharePointAuth { get; set; } = new();
    }

    public class DiagnosticConfiguration
    {
        public string SharePointBaseUrl { get; set; } = string.Empty;
        public string SharePointSiteUrl { get; set; } = string.Empty;
        public string SharePointListName { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public bool ClientSecretConfigured { get; set; }
    }

    public class SharePointAuthResult
    {
        public string Status { get; set; } = string.Empty;
        public bool TokenReceived { get; set; }
        public int TokenLength { get; set; }
        public string? Error { get; set; }
        public string? InnerException { get; set; }
    }
}