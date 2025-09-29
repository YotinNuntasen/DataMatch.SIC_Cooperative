using Azure.Data.Tables;
using System.Text.Json.Serialization;

namespace DataMatchBackend.Models;

/// <summary>
/// Customer data entity for Azure Table Storage
/// </summary>
public class CustomerDataEntity : ITableEntity
{
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = "FromSQL";
    
    [JsonPropertyName("rowKey")]
    public string RowKey { get; set; } = "";
    
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }
    
    [JsonPropertyName("eTag")]
    public Azure.ETag ETag { get; set; }

    // Customer properties
    [JsonPropertyName("custShortDimName")]
    public string CustShortDimName { get; set; } = "";
    
    [JsonPropertyName("postingDate")]
    public DateTime? PostingDate { get; set; }
    
    [JsonPropertyName("prefixdocumentNo")]
    public string PrefixdocumentNo { get; set; } = "";
    
    [JsonPropertyName("selltoCustName_SalesHeader")]
    public string SelltoCustName_SalesHeader { get; set; } = "";
    
    [JsonPropertyName("systemRowVersion")]
    public string SystemRowVersion { get; set; } = "";
    
    [JsonPropertyName("documentDate")]
    public DateTime? documentDate { get; set; }
    
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
    
    [JsonPropertyName("srodocumentNo")]
    public string SrodocumentNo { get; set; } = "";
    [JsonPropertyName("description")]
    public string description { get; set; } = "";
    [JsonPropertyName("unitOfMeasure")]
    public string unitOfMeasure { get; set; } = "";
    [JsonPropertyName("unitPrice")]
    public double unitPrice { get; set; } = 0;
    [JsonPropertyName("lineDiscount")]
    public double LineDiscount { get; set; } = 0;
    [JsonPropertyName("lineAmount")]
    public double lineAmount { get; set; } = 0;
    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = "";
    [JsonPropertyName("currencyRate")]
    public double CurrencyRate { get; set; } = 0;
    [JsonPropertyName("salesPerUnit")]
    public double SalesPerUnit { get; set; } = 0;
    [JsonPropertyName("totalSales")]
    public double TotalSales { get; set; } = 0;
    [JsonPropertyName("podocumentNo")]
    public string PodocumentNo { get; set; } = "";
    [JsonPropertyName("shortcutDimension1Code")]
    
    public string ShortcutDimension1Code { get; set; } = "";
    [JsonPropertyName("shortcutDimension2Code")]
    public string ShortcutDimension2Code { get; set; } = "";
    [JsonPropertyName("itemprodDetail2")]
    public string ItemprodDetail2 { get; set; } = "";
    [JsonPropertyName("custAppDimName")]
    public string CustAppDimName { get; set; } = "";
    [JsonPropertyName("prodChipNameDimName")]
    public string ProdChipNameDimName { get; set; } = "";

    [JsonPropertyName("regionDimName3")]
    public string RegionDimName3 { get; set; } = "";

    [JsonPropertyName("salespersonDimName")]
    public string SalespersonDimName { get; set; } = "";

    [JsonPropertyName("dataSources")]
    public string DataSources { get; set; } = "";

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("modified")]
    public DateTime? Modified { get; set; }

    // Helper methods
    public Dictionary<string, object> ToSearchableDictionary()
    {
        return new Dictionary<string, object>
        {
            { "rowKey", RowKey },
            { "custShortDimName", CustShortDimName },
            { "postingDate", PostingDate },
            { "prefixdocumentNo", PrefixdocumentNo },
            { "selltoCustName_SalesHeader", SelltoCustName_SalesHeader },
            { "systemRowVersion", SystemRowVersion },
            { "documentDate", documentDate },
            { "documentNo", documentNo },
            { "itemReferenceNo", itemReferenceNo },
            { "lineNo", lineNo },
            { "no", no },
            { "quantity", quantity },
            { "sellToCustomerNo", sellToCustomerNo },
            { "shipmentNo", shipmentNo },
            { "sodocumentNo", sodocumentNo },
            { "srodocumentNo", SrodocumentNo },
            { "description", description },
            { "unitOfMeasure", unitOfMeasure },
            { "unitPrice", unitPrice },
            { "lineDiscount", LineDiscount },
            { "lineAmount", lineAmount },
            { "currencyCode", CurrencyCode },
            { "currencyRate", CurrencyRate },
            { "salesPerUnit", SalesPerUnit },
            { "totalSales", TotalSales },
            { "podocumentNo", PodocumentNo },
            { "shortcutDimension1Code", ShortcutDimension1Code },
            { "shortcutDimension2Code", ShortcutDimension2Code },
            { "ItemprodDetail2", ItemprodDetail2 },
            { "CustAppDimName", CustAppDimName },
            { "ProdChipNameDimName", ProdChipNameDimName },
            { "RegionDimName3", RegionDimName3 },
            { "SalespersonDimName", SalespersonDimName },
            { "created", Created },
            { "modified", Modified },
        };
    }

    public void UpdateTimestamp()
    {
        Modified = DateTime.UtcNow;
    }

   
}