// Models/SharePointList.cs
using System.Text.Json.Serialization;

namespace DataMatchBackend.Models
{
    /// <summary>
    /// Represents metadata for a single SharePoint list.
    /// </summary>
    public class SharePointList
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("createdDateTime")]
        public DateTime? CreatedDateTime { get; set; }

        [JsonPropertyName("lastModifiedDateTime")]
        public DateTime? LastModifiedDateTime { get; set; }

        [JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }
        
        [JsonPropertyName("itemCount")]
        public int ItemCount { get; set; }
    }
}