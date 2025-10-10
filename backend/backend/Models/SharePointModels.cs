using System;
using System.Text.Json.Serialization;

namespace DataMatchBackend.Models
{
    /// <summary>
    /// SharePoint Opportunity List contact model
    /// Updated to match actual SharePoint list schema with all fields
    /// </summary>
    public class SharePointContact
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("caeInCharge")] 
        public string CaeInCharge { get; set; } = "";

        private string _name = "";

        // Core Opportunity Fields
        [JsonPropertyName("opportunityId")]
        public string opportunityId { get; set; } = "";

        [JsonPropertyName("opportunityName")]
        public string opportunityName { get; set; } = "";

       
        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = ""; 
        [JsonPropertyName("customerNameSalePersonCode")]
        public string CustomerNameSalePersonCode { get; set; } = "";
        [JsonIgnore]
        public int? SalePersonId { get; set; }

        [JsonIgnore]
        public int? CustomerId { get; set; }
        

        // Filter and Tags
        [JsonPropertyName("filterTag")]
        public string FilterTag { get; set; } = "";

        // Financial Fields
        [JsonPropertyName("expectedRevenueUSD")]
        public decimal ExpectedRevenueUSD { get; set; } = 0;
        
        [JsonPropertyName("calculatedExpectedRevenue")] // <--- แก้ไขชื่อจาก calExpectedRevenue
        public decimal CalculatedExpectedRevenue { get; set; } = 0;
        
        [JsonPropertyName("expectedVolume")]
        public decimal ExpectedVolume { get; set; } = 0;

        [JsonPropertyName("targetPrice")]
        public decimal TargetPrice { get; set; } = 0;

        // Process and Status Fields
        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "";

        [JsonPropertyName("pipelineStage")]
        public string PipelineStage { get; set; } = "";

        [JsonPropertyName("productGroup")]
        public string ProductGroup { get; set; } = "";

        [JsonPropertyName("activityStatus")]
        public string ActivityStatus { get; set; } = "";

        [JsonPropertyName("actionOwner")]
        public string ActionOwner { get; set; } = "";

        // Product Information
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = "";

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = "";

        // Key Dates
        [JsonPropertyName("registerDate")]
        public DateTime? RegisterDate { get; set; }

        [JsonPropertyName("nextActionDate")]
        public DateTime? NextActionDate { get; set; }
        
        [JsonPropertyName("massProductionDate")] // <--- แก้ไขชื่อ
        public DateTime? MassProductionDate { get; set; }

   
        [JsonPropertyName("step2EntryDate")]  // 
        public DateTime? Step2EntryDate { get; set; }

        [JsonPropertyName("step3EntryDate")] // <--- แก้ไขชื่อ และเปลี่ยนเป็น Nullable
        public DateTime? Step3EntryDate { get; set; }

        [JsonPropertyName("step4EntryDate")] // <--- แก้ไขชื่อ และเปลี่ยนเป็น Nullable
        public DateTime? Step4EntryDate { get; set; }
        
        [JsonPropertyName("step5EntryDate")] // <--- แก้ไขชื่อ และเปลี่ยนเป็น Nullable
        public DateTime? Step5EntryDate { get; set; }

        [JsonPropertyName("s6EvalEntryDate")] // <--- เปลี่ยนเป็น Nullable
        public DateTime? S6EvalEntryDate { get; set; }

        [JsonPropertyName("s7DIEntryDate")] // <--- เปลี่ยนเป็น Nullable
        public DateTime? S7DIEntryDate { get; set; }

        [JsonPropertyName("s8PreProEntryDate")] // <--- เปลี่ยนเป็น Nullable
        public DateTime? S8PreProEntryDate { get; set; }

        [JsonPropertyName("s9DWINEntryDate")] // <--- เปลี่ยนเป็น Nullable
        public DateTime? S9DWINEntryDate { get; set; }

        // Closed Lost Information
        [JsonPropertyName("isClosedLost")] // <--- แก้ไขชื่อ
        public bool IsClosedLost { get; set; } = false;

        [JsonPropertyName("closedLostDate")]
        public DateTime? ClosedLostDate { get; set; }

        [JsonPropertyName("closedLostReason")]
        public string ClosedLostReason { get; set; } = "";

        [JsonPropertyName("closedLostCommonReason")] // <--- แก้ไขชื่อ
        public string ClosedLostCommonReason { get; set; } = "";

        // Suspend Information
        [JsonPropertyName("isSuspended")]
    public bool IsSuspended { get; set; } = false;

        [JsonPropertyName("suspendReason")]
        public string SuspendReason { get; set; } = "";

        // Lead Information
        [JsonPropertyName("sourceOfLead")]
        public string SourceOfLead { get; set; } = "";

        // Distributor (Lookup)
        [JsonPropertyName("distributorName")] // <--- แก้ไขชื่อ
        public string DistributorName { get; set; } = "";

        // Comments and Notes
        [JsonPropertyName("note")]
        public string Note { get; set; } = "";

        [JsonPropertyName("multilineComment")] // <--- เพิ่มเข้ามา
        public string MultilineComment { get; set; } = "";

        [JsonPropertyName("dailyLatestComment")]
        public string DailyLatestComment { get; set; } = "";

        // System Fields
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("modified")]
        public DateTime Modified { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("modifiedBy")]
        public string ModifiedBy { get; set; } = "";

        // Standard Contact Fields (for compatibility)
        // [JsonPropertyName("name")]
        // public string Name
        // {
        //     get => !string.IsNullOrEmpty(_name) ? _name :
        //            (!string.IsNullOrEmpty(CustomerName) ? CustomerName : OpportunityName);
        //     set => _name = value;
        // }

        // [JsonPropertyName("country")]
        // public string Country { get; set; } = "";

        // [JsonPropertyName("industry")]
        // public string Industry
        // {
        //     get => ProductGroup;
        //     set { /* Derived from ProductGroup */ }
        // }

        // [JsonPropertyName("website")]
        // public string Website { get; set; } = "";

        // [JsonPropertyName("contactPerson")]
        // public string ContactPerson { get; set; } = "";

        // [JsonPropertyName("notes")]
        // public string Notes
        // {
        //     get => Note;
        //     set => Note = value;
        // }

        // [JsonPropertyName("status")]
        // public string Status
        // {
        //     get => ActivityStatus ?? "Active";
        //     set { /* Derived from ActivityStatus */ }
        // }

        // [JsonPropertyName("lastModified")]
        // public DateTime LastModified
        // {
        //     get => Modified;
        //     set => Modified = value;
        // }

        // [JsonPropertyName("source")]
        // public string Source { get; set; } = "sharepoint";

        // [JsonPropertyName("sourceList")]
        // public string SourceList { get; set; } = "Opportunity_List";

        // [JsonPropertyName("contactType")]
        // public string ContactType { get; set; } = "Opportunity";

        // // Revenue alias for compatibility
        // [JsonPropertyName("expectedRevenue")]
        // public decimal ExpectedRevenue
        // {
        //     get => ExpectedRevenueUSD;
        //     set => ExpectedRevenueUSD = value;
        // }

        // // Additional computed properties
        // [JsonPropertyName("currentStage")]
        // public string CurrentStage => PipelineStage;

        // [JsonPropertyName("isActive")]
        // public bool IsActive => !IsClosedLost && !IsSuspended; // <--- แก้ไขให้ใช้ IsClosedLost

        // [JsonPropertyName("daysSinceRegistration")]
        // public int? DaysSinceRegistration
        // {
        //     get => RegisterDate.HasValue ?
        //            (int)(DateTime.Now - RegisterDate.Value).TotalDays : null;
        // }

        // [JsonPropertyName("daysSinceLastAction")]
        // public int? DaysSinceLastAction
        // {
        //     get => NextActionDate.HasValue ?
        //            (int)(DateTime.Now - NextActionDate.Value).TotalDays : null;
        // }

        // [JsonPropertyName("isOverdue")]
        // public bool IsOverdue
        // {
        //     get => NextActionDate.HasValue && NextActionDate.Value < DateTime.Now;
        // }

        // // Summary fields for display
        // [JsonPropertyName("summary")]
        // public string Summary
        // {
        //     get => $"{OpportunityName} - {CustomerName} ({PipelineStage})";
        // }

        [JsonPropertyName("revenueFormatted")]
        public string RevenueFormatted
        {
            get => ExpectedRevenueUSD > 0 ?
                   $"${ExpectedRevenueUSD:N0} USD" : "TBD";
        }

        [JsonPropertyName("priorityLevel")]
        public int PriorityLevel
        {
            get => Priority?.ToLower() switch
            {
                "high" => 3,
                "medium" => 2,
                "low" => 1,
                _ => 0
            };
        }
    }
}