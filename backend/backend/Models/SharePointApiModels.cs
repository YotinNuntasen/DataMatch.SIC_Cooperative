using System.Text.Json.Serialization;

namespace DataMatchBackend.Models;

public class SharePointRestResponse<T>
{
    [JsonPropertyName("d")]
    public SharePointRestData<T>? D { get; set; }
    
    public List<T> GetResults() => D?.Results ?? new List<T>();
}

public class SharePointRestData<T>
{
    [JsonPropertyName("results")]
    public List<T> Results { get; set; } = new();
}

public class SharePointSingleItemResponse
{
    [JsonPropertyName("d")]
    public SharePointRestItem? D { get; set; }
}

public class SharePointRestItem
{
    public int? Id { get; set; }
    public string? Title { get; set; }
    public string? OpportunityName { get; set; }
    public string? OpportunityID { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerNameSalePersonCode { get; set; }
    public string? CustomerNameCustomerName { get; set; }
    public decimal? ExpectedRevenueUSD { get; set; }
    public decimal? TargetPrice { get; set; }
    public decimal? EstimatedRevenue { get; set; }
    public string? Priority { get; set; }
    public string? PipelineStage { get; set; }
    public string? ProductGroup { get; set; }
    public string? ActivityStatus { get; set; }
    public string? RegisterDate { get; set; }
    public string? NextActionDate { get; set; }
    public string? TargetMassProductionDate { get; set; }
    public string? SourceofLead { get; set; }
    public bool? ClosedLost { get; set; }
    public string? ClosedLostReason { get; set; }
    public string? Note { get; set; }
    public string? MultihosComment { get; set; }
    public string? DailyObjectComment { get; set; }
    public string? Created { get; set; }
    public string? Modified { get; set; }
    public SharePointRestUser? Author { get; set; }
    public SharePointRestUser? Editor { get; set; }
}

public class SharePointRestListInfo
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? ItemCount { get; set; }
    public string? Created { get; set; }
    public string? LastItemModifiedDate { get; set; }
    public bool? Hidden { get; set; }
}

public class SharePointRestUser
{
    public string? Title { get; set; }
    public string? LoginName { get; set; }
}