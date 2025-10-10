namespace DataMatchBackend.Models;

/// <summary>
/// Converter methods between different model types
/// </summary>
public static class ModelConverters
{
    
public static PersonDocument ToPersonDocument(this CustomerDataEntity customer)
{
    return new PersonDocument
    {
        RowKey = customer.RowKey,
        PartitionKey = "MergedCustomer",
        
        OpportunityId = "",
        OpportunityName = "",
        
        CustShortDimName = customer.CustShortDimName,
        PrefixdocumentNo = customer.PrefixdocumentNo,
        SelltoCustName_SalesHeader = customer.SelltoCustName_SalesHeader,
        SystemRowVersion = customer.SystemRowVersion,
        DocumentDate = customer.documentDate,
        documentNo = customer.documentNo,
        itemReferenceNo = customer.itemReferenceNo,
        lineNo = customer.lineNo,
        no = customer.no,
        quantity = customer.quantity,
        sellToCustomerNo = customer.sellToCustomerNo,
        shipmentNo = customer.shipmentNo,
        sodocumentNo = customer.sodocumentNo,
        description = customer.description,
        unitPrice = customer.unitPrice,
        LineDiscount = customer.LineDiscount,
        lineAmount = customer.lineAmount,
        CurrencyRate = customer.CurrencyRate,
        SalesPerUnit = customer.SalesPerUnit,
        TotalSales = customer.TotalSales,
        CustAppDimName = customer.CustAppDimName, 
        ProdChipNameDimName = customer.ProdChipNameDimName,
        RegionDimName3 = customer.RegionDimName3,
        SalespersonDimName = customer.SalespersonDimName, 
        Created = customer.Created,
        Modified = customer.Modified,
    };
}

    /// <summary>
    /// Convert CustomerDataEntity and SharePoint to PersonDocument
    /// </summary>
    public static PersonDocument ToPersonDocument(this CustomerDataEntity customer, SharePointContact sharePointContact)
    {
        return new PersonDocument
        {
            RowKey = customer.RowKey,
            PartitionKey = "MergedCustomer",
            OpportunityId = sharePointContact.opportunityId,
            OpportunityName = sharePointContact.opportunityName,
            CustShortDimName = customer.CustShortDimName,
            PrefixdocumentNo = customer.PrefixdocumentNo,
            SelltoCustName_SalesHeader = customer.SelltoCustName_SalesHeader,
            SystemRowVersion = customer.SystemRowVersion,
            DocumentDate = customer.documentDate,
            documentNo = customer.documentNo,
            itemReferenceNo = customer.itemReferenceNo,
            lineNo = customer.lineNo,
            no = customer.no,
            quantity = customer.quantity,
            sellToCustomerNo = customer.sellToCustomerNo,
            shipmentNo = customer.shipmentNo,
            sodocumentNo = customer.sodocumentNo,

            description = customer.description,
            unitPrice = customer.unitPrice,
            LineDiscount = customer.LineDiscount,
            lineAmount = customer.lineAmount,
            CurrencyRate = customer.CurrencyRate,
            SalesPerUnit = customer.SalesPerUnit,
            TotalSales = customer.TotalSales,
            CustAppDimName = customer.CustAppDimName,
            ProdChipNameDimName = customer.ProdChipNameDimName,
            RegionDimName3 = customer.RegionDimName3,
            SalespersonDimName = customer.SalespersonDimName,
            Created = customer.Created,
            Modified = customer.Modified,
        };
    }

    /// <summary>
    /// Enhanced merge with conflict resolution
    /// </summary>
    public static PersonDocument MergeWithSharePointData(this PersonDocument customer, SharePointContact sharePointContact)
    {

        var merged = new PersonDocument
        {
            RowKey = customer.RowKey,
            PartitionKey = "MergedCustomer",
            documentNo = customer.documentNo,

            // Use Azure data as primary, SharePoint as secondary
            CustShortDimName = !string.IsNullOrEmpty(customer.CustShortDimName) ? customer.CustShortDimName : sharePointContact.CustomerName,
            SalespersonDimName = !string.IsNullOrEmpty(customer.SalespersonDimName) ? customer.SalespersonDimName : sharePointContact.CustomerNameSalePersonCode,
            ProdChipNameDimName = !string.IsNullOrEmpty(customer.ProdChipNameDimName) ? customer.ProdChipNameDimName : sharePointContact.ProductName,

            // Keep Azure fields
            PrefixdocumentNo = customer.PrefixdocumentNo,
            SelltoCustName_SalesHeader = customer.SelltoCustName_SalesHeader,
            SystemRowVersion = customer.SystemRowVersion,
            DocumentDate = customer.DocumentDate,
            itemReferenceNo = customer.itemReferenceNo,
            lineNo = customer.lineNo,
            no = customer.no,
            quantity = customer.quantity,
            sellToCustomerNo = customer.sellToCustomerNo,
            shipmentNo = customer.shipmentNo,
            sodocumentNo = customer.sodocumentNo,
            description = customer.description,
            unitPrice = customer.unitPrice,
            LineDiscount = customer.LineDiscount,
            lineAmount = customer.lineAmount,
            CurrencyRate = customer.CurrencyRate,
            SalesPerUnit = customer.SalesPerUnit,
            TotalSales = customer.TotalSales,
            CustAppDimName = customer.CustAppDimName,
            RegionDimName3 = customer.RegionDimName3,
            OpportunityId = sharePointContact.opportunityId,
            OpportunityName = sharePointContact.opportunityName,

            Created = customer.Created,
            Modified = DateTime.UtcNow,
        };
        return merged;
    }

    /// <summary>
    /// Detect data conflicts between Azure and SharePoint records
    /// </summary>

    /// <summary>
    /// Convert PersonDocument to CustomerDataEntity
    /// </summary>
    public static CustomerDataEntity ToCustomerDataEntity(this PersonDocument person)
    {
        return new CustomerDataEntity
        {
            RowKey = person.RowKey,
            PartitionKey = "FromSQL",
            CustShortDimName = person.CustShortDimName,
            PrefixdocumentNo = person.PrefixdocumentNo,
            SelltoCustName_SalesHeader = person.SelltoCustName_SalesHeader,
            SystemRowVersion = person.SystemRowVersion,
            documentDate = person.DocumentDate,
            documentNo = person.documentNo,
            itemReferenceNo = person.itemReferenceNo,
            lineNo = person.lineNo,
            no = person.no,
            quantity = person.quantity,
            sellToCustomerNo = person.sellToCustomerNo,
            shipmentNo = person.shipmentNo,
            sodocumentNo = person.sodocumentNo,
            Created = person.Created,
            Modified = person.Modified,
            description = person.description,
            unitPrice = person.unitPrice,
            LineDiscount = person.LineDiscount,
            lineAmount = person.lineAmount,
            CurrencyRate = person.CurrencyRate,
            SalesPerUnit = person.SalesPerUnit,
            TotalSales = person.TotalSales,
            CustAppDimName = person.CustAppDimName,
            ProdChipNameDimName = person.ProdChipNameDimName,
            RegionDimName3 = person.RegionDimName3,
            SalespersonDimName = person.SalespersonDimName,
        };
    }

    /// <summary>
    /// Convert SharePointContact to PersonDocument for matching
    /// </summary>
    public static PersonDocument ToPersonDocument(this SharePointContact contact)
    {
        return new PersonDocument
        {
            RowKey = Guid.NewGuid().ToString(),

            CustShortDimName = contact.CustomerName,
            sellToCustomerNo = contact.CustomerNameSalePersonCode ?? "",
            ProdChipNameDimName = contact.ProductName ?? "",
            CustAppDimName = contact.ProductGroup,
            Created = contact.Created,
            Modified = contact.Modified,

        };
    }

    /// <summary>
    /// Convert list of CustomerDataEntity to PersonDocument
    /// </summary>
    /// <summary>
    /// Batch convert with progress tracking
    /// </summary>
    public static List<PersonDocument> ToPersonDocuments(this List<CustomerDataEntity> customers,
        Action<int, int>? progressCallback = null)
    {
        var result = new List<PersonDocument>();

        for (int i = 0; i < customers.Count; i++)
        {
            result.Add(customers[i].ToPersonDocument());
            progressCallback?.Invoke(i + 1, customers.Count);
        }

        return result;
    }

    /// <summary>
    /// Convert list of PersonDocument to CustomerDataEntity
    /// </summary>
    public static List<CustomerDataEntity> ToCustomerDataEntities(this List<PersonDocument> persons)
    {
        return persons.Select(p => p.ToCustomerDataEntity()).ToList();
    }
}