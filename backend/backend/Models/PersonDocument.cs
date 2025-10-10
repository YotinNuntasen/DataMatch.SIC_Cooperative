using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DataMatchBackend.JsonConverters;

namespace DataMatchBackend.Models;

/// <summary>
/// Person document for merged data in Azure Table Storage
/// Table: mergentbowitherp
/// </summary>
public class PersonDocument : ITableEntity
{

    public string PartitionKey { get; set; } = "MergedCustomer";
    public string RowKey { get; set; } = "null";

    [JsonPropertyName("opportunityId")] 
    public string OpportunityId { get; set; } = "";

    [JsonPropertyName("opportunityName")] 
    public string OpportunityName { get; set; } = "";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Customer Core Information
    [JsonPropertyName("custShortDimName")]
    public string CustShortDimName { get; set; } = "";

    [JsonPropertyName("prefixdocumentNo")]
    public string PrefixdocumentNo { get; set; } = "";

    [JsonPropertyName("selltoCustName_SalesHeader")]
    public string SelltoCustName_SalesHeader { get; set; } = "";

    [JsonPropertyName("systemRowVersion")]
    public string SystemRowVersion { get; set; } = "";

    [JsonPropertyName("documentDate")]
    [JsonConverter(typeof(NullableDateTimeConverter))]
    public DateTime? DocumentDate { get; set; }
    [JsonPropertyName("documentNo")]
    public string documentNo { get; set; } = "";

    [JsonPropertyName("itemReferenceNo")]
    public string itemReferenceNo { get; set; } = "";

    [JsonPropertyName("lineNo")]
    public int lineNo { get; set; } = 0;

    [JsonPropertyName("no")]
    public string no { get; set; } = "";

    [JsonPropertyName("quantity")]
    public double quantity { get; set; } = 0;

    [JsonPropertyName("sellToCustomerNo")]
    public string sellToCustomerNo { get; set; } = "";

    [JsonPropertyName("shipmentNo")]
    public string shipmentNo { get; set; } = "";

    [JsonPropertyName("sodocumentNo")]
    public string sodocumentNo { get; set; } = "";
    [JsonPropertyName("description")]
    public string description { get; set; } = "";
    [JsonPropertyName("unitPrice")]
    public double unitPrice { get; set; } = 0;
    [JsonPropertyName("lineDiscount")]
    public double LineDiscount { get; set; } = 0;
    [JsonPropertyName("lineAmount")]
    public double lineAmount { get; set; } = 0;
    [JsonPropertyName("currencyRate")]
    public double CurrencyRate { get; set; } = 0;
    [JsonPropertyName("salesPerUnit")]
    public double SalesPerUnit { get; set; } = 0;
    [JsonPropertyName("totalSales")]
    public double TotalSales { get; set; } = 0;

    [JsonPropertyName("custAppDimName")]
    public string CustAppDimName { get; set; } = "";
    [JsonPropertyName("prodChipNameDimName")]
    public string ProdChipNameDimName { get; set; } = "";

    [JsonPropertyName("regionDimName3")]
    public string RegionDimName3 { get; set; } = "";

    [JsonPropertyName("salespersonDimName")]
    public string SalespersonDimName { get; set; } = "";

    [JsonPropertyName("created")]
    [JsonConverter(typeof(NullableDateTimeConverter))]
    public DateTime? Created { get; set; }

    [JsonPropertyName("modified")]
    [JsonConverter(typeof(NullableDateTimeConverter))]
    public DateTime? Modified { get; set; }


}