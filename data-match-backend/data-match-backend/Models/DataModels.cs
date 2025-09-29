using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Data.Tables;

namespace DataMatchBackend.Models;

/// <summary>
/// Search criteria for customer data
/// </summary>
public class SearchCriteria
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = "";
    
    [JsonPropertyName("country")]
    public string Country { get; set; } = "";
    
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; } = 1;
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 50;
    
}

/// <summary>
/// Bulk update request for customer data
/// </summary>
public class BulkUpdateRequest
{
    [JsonPropertyName("records")]
    public List<PersonDocument> Records { get; set; } = new();  // เปลี่ยนจาก CustomerDataEntity เป็น PersonDocument
    
    [JsonPropertyName("validateData")]
    public bool ValidateData { get; set; } = true;
    
    [JsonPropertyName("createBackup")]
    public bool CreateBackup { get; set; } = true;
}

/// <summary>
/// Matched record for data matching
/// </summary>
public class MatchedRecord
{
    
    [JsonPropertyName("sourceRecord")]
    public CustomerDataEntity? SourceRecord { get; set; }
    
    [JsonPropertyName("targetRecord")]
    public SharePointContact? TargetRecord { get; set; }
    
    [JsonPropertyName("similarityScore")]
    public double SimilarityScore { get; set; }
    
    [JsonPropertyName("matchType")]
    public string MatchType { get; set; } = ""; // Auto, Manual, Suggested
    
    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = ""; // High, Medium, Low
    
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = "";
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = "";
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Properties ที่ code เก่าใช้งาน
    [JsonPropertyName("sharePointData")]
    public SharePointContact? SharePointData
    {
        get => TargetRecord;
        set => TargetRecord = value;
    }

    [JsonPropertyName("azureData")]
    public CustomerDataEntity? AzureData
    {
        get => SourceRecord;
        set => SourceRecord = value;
    }

    [JsonPropertyName("matchedBy")]
    public string MatchedBy
    {
        get => CreatedBy;
        set => CreatedBy = value;
    }

    [JsonPropertyName("matchDetails")]
    public Dictionary<string, object> MatchDetails
    {
        get => Metadata;
        set => Metadata = value;
    }
}

/// <summary>
/// Match record entity for Azure Table Storage
/// </summary>
public class MatchRecord : ITableEntity
{
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = "MatchRecord";
    
    [JsonPropertyName("rowKey")]
    public string RowKey { get; set; } = "";
    
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }
    
    [JsonPropertyName("eTag")]
    public Azure.ETag ETag { get; set; }
    
    [JsonPropertyName("sourceRecordId")]
    public string SourceRecordId { get; set; } = "";
    
    [JsonPropertyName("targetRecordId")]
    public string TargetRecordId { get; set; } = "";
    
    [JsonPropertyName("similarityScore")]
    public double SimilarityScore { get; set; }
    
    [JsonPropertyName("matchType")]
    public string MatchType { get; set; } = "";
    
    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = "";
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";
    
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = "";
    
    [JsonPropertyName("approvedDate")]
    public DateTime? ApprovedDate { get; set; }
    
    [JsonPropertyName("approvedBy")]
    public string ApprovedBy { get; set; } = "";
    
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = "";
    
    [JsonPropertyName("matchedFieldsJson")]
    public string MatchedFieldsJson { get; set; } = "";
    
    [JsonPropertyName("metadataJson")]
    public string MetadataJson { get; set; } = "";


    // Helper methods
    public void SetMetadata(Dictionary<string, object> metadata)
    {
        MetadataJson = JsonSerializer.Serialize(metadata);
    }
}

/// <summary>
/// Data statistics model
/// </summary>
public class DataStatistics
{
    [JsonPropertyName("totalCustomers")]
    public int TotalCustomers { get; set; }
    
    [JsonPropertyName("activeCustomers")]
    public int ActiveCustomers { get; set; }
    
    [JsonPropertyName("inactiveCustomers")]
    public int InactiveCustomers { get; set; }
    
    [JsonPropertyName("totalMatches")]
    public int TotalMatches { get; set; }
    
    [JsonPropertyName("pendingMatches")]
    public int PendingMatches { get; set; }
    
    [JsonPropertyName("approvedMatches")]
    public int ApprovedMatches { get; set; }
    
    [JsonPropertyName("rejectedMatches")]
    public int RejectedMatches { get; set; }
    
    [JsonPropertyName("recentCustomers")]
    public int RecentCustomers { get; set; }
    
    [JsonPropertyName("recentMatches")]
    public int RecentMatches { get; set; }
    
    [JsonPropertyName("averageSimilarityScore")]
    public double AverageSimilarityScore { get; set; }
    
    [JsonPropertyName("topRegion")]
    public Dictionary<string, int> TopRegion { get; set; } = new();
    
    [JsonPropertyName("topIndustries")]
    public Dictionary<string, int> TopIndustries { get; set; } = new();
    
    [JsonPropertyName("customerTypes")]
    public Dictionary<string, int> CustomerTypes { get; set; } = new();
    
    [JsonPropertyName("monthlyGrowth")]
    public Dictionary<string, int> MonthlyGrowth { get; set; } = new();
    
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Properties ที่ code เก่าใช้งาน
    [JsonPropertyName("totalRecords")]
    public int TotalRecords => TotalCustomers;

    [JsonPropertyName("recentlyModifiedCount")]
    public int RecentlyModifiedCount => RecentCustomers;

    [JsonPropertyName("dataCompletenessAverage")]
    public double DataCompletenessAverage { get; set; } = 85.5;

    [JsonPropertyName("matchTypeBreakdown")]
    public Dictionary<string, int> MatchTypeBreakdown { get; set; } = new();

    [JsonPropertyName("lastCalculated")]
    public DateTime LastCalculated => LastUpdated;
}