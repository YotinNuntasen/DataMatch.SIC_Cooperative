using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using DataMatchBackend.Models;
using DataMatchBackend.Services;
namespace DataMatchBackend.Services;

public class TableStorageService : IDataService
{
    private readonly TableClient _customerTableClient;
    private readonly TableClient _personDocumentTableClient;
    private readonly TableClient _matchTableClient;
    private readonly ILogger<TableStorageService> _logger;
    private readonly string _connectionString;


    // ในไฟล์ Services/TableStorageService.cs

    public async Task<(int deletedCount, int insertedCount)> ReplaceAllPersonDocumentsAsync(List<PersonDocument> newPersons)
    {
        // ✅ --- นี่คือจุดที่แก้ไข ---
        // บังคับให้ฟังก์ชันทำงานกับ PartitionKey ที่ถูกต้อง ("FromSQL") เสมอ
        // ไม่ว่าข้อมูลที่ส่งมาจาก Frontend จะมีค่าอะไรก็ตาม
        string partitionKey = "FromSQL";
        // -------------------------

        _logger.LogInformation("Starting Replace operation on PartitionKey '{PartitionKey}' with {NewCount} new records.", partitionKey, newPersons.Count);

        var allExistingRecords = new List<ITableEntity>();

        // 1. ดึงข้อมูลเก่าทั้งหมดจาก PartitionKey "FromSQL"
        _logger.LogInformation("Fetching all existing documents with PartitionKey: {PartitionKey}", partitionKey);
        await foreach (var entity in _personDocumentTableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{partitionKey}'"))
        {
            allExistingRecords.Add(entity);
        }
        _logger.LogInformation("Found {ExistingCount} existing records to delete.", allExistingRecords.Count);

        var deletedCount = 0;
        var insertedCount = 0;

        // 2. ลบข้อมูลเก่าทั้งหมด (Batch Delete)
        if (allExistingRecords.Any())
        {
            var deleteTasks = new List<Task<Response<IReadOnlyList<Response>>>>();
            var batch = new List<TableTransactionAction>();

            foreach (var record in allExistingRecords)
            {
                batch.Add(new TableTransactionAction(TableTransactionActionType.Delete, record));
                if (batch.Count == 100)
                {
                    deleteTasks.Add(_personDocumentTableClient.SubmitTransactionAsync(batch));
                    batch = new List<TableTransactionAction>();
                }
            }
            if (batch.Any())
            {
                deleteTasks.Add(_personDocumentTableClient.SubmitTransactionAsync(batch));
            }

            var responses = await Task.WhenAll(deleteTasks);
            // ตรวจสอบว่าทุก transaction สำเร็จ
            if (responses.All(r => !r.HasValue || (r.Value != null && !r.GetRawResponse().IsError)))
            {
                deletedCount = allExistingRecords.Count;
                _logger.LogInformation("Successfully deleted {DeletedCount} records from PartitionKey '{PartitionKey}'.", deletedCount, partitionKey);
            }
            else
            {
                _logger.LogError("One or more delete transactions failed during replace operation.");
                throw new Exception("Failed to delete existing records during replace operation.");
            }
        }

        // 3. เพิ่มข้อมูลใหม่ทั้งหมด (Batch Insert)
        if (newPersons.Any())
        {
            var insertTasks = new List<Task<Response<IReadOnlyList<Response>>>>();
            var insertBatch = new List<TableTransactionAction>();

            foreach (var record in newPersons)
            {
                // บังคับให้ PartitionKey เป็นค่าที่ถูกต้องเสมอ
                record.PartitionKey = partitionKey;
                if (string.IsNullOrEmpty(record.RowKey))
                {
                    record.RowKey = Guid.NewGuid().ToString();
                }
                record.Timestamp = DateTimeOffset.UtcNow;

                insertBatch.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, record));
                if (insertBatch.Count == 100)
                {
                    insertTasks.Add(_personDocumentTableClient.SubmitTransactionAsync(insertBatch));
                    insertBatch = new List<TableTransactionAction>();
                }
            }
            if (insertBatch.Any())
            {
                insertTasks.Add(_personDocumentTableClient.SubmitTransactionAsync(insertBatch));
            }

            await Task.WhenAll(insertTasks);
            insertedCount = newPersons.Count;
            _logger.LogInformation("Successfully inserted {InsertedCount} new records into PartitionKey '{PartitionKey}'.", insertedCount, partitionKey);
        }

        return (deletedCount, insertedCount);
    }

    public async Task<List<PersonDocument>> GetPersonDocumentsByOpportunityIdAsync(string opportunityId)
    {
        if (string.IsNullOrEmpty(opportunityId))
        {
            throw new ArgumentNullException(nameof(opportunityId), "OpportunityId cannot be null or empty.");
        }
        List<PersonDocument> results = new List<PersonDocument>();
        string filter = $"OpportunityId eq '{opportunityId}'";
        await foreach (PersonDocument entity in _personDocumentTableClient.QueryAsync<PersonDocument>(filter))
        {
            results.Add(entity);
        }

        return results;
    }


    public async Task<PersonDocument> UpsertPersonDocumentAsync(PersonDocument person)
    {
        try
        {

            if (string.IsNullOrEmpty(person.PartitionKey))
                person.PartitionKey = "MergedCustomer";

            if (string.IsNullOrEmpty(person.RowKey))
            {
                person.RowKey = Guid.NewGuid().ToString();
                person.Created = DateTime.UtcNow;
            }

            person.Modified = DateTime.UtcNow;
            person = NormalizeDateTimesToUtc(person);

            _logger.LogInformation("Saving PersonDocument: RowKey={RowKey}, Customer={Customer}, DocumentNo={DocumentNo}",
                person.RowKey, person.CustShortDimName, person.documentNo);

            await _personDocumentTableClient.UpsertEntityAsync(person, TableUpdateMode.Replace);

            _logger.LogInformation("Successfully upserted PersonDocument with RowKey: {RowKey}", person.RowKey);
            return person;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting person document with RowKey: {RowKey}", person.RowKey);
            throw;
        }
    }
    /// <summary>
    /// แปลง DateTime ทั้งหมดให้เป็น UTC
    /// </summary>
    private PersonDocument NormalizeDateTimesToUtc(PersonDocument person)
    {

        person.DocumentDate = NormalizeDateTime(person.DocumentDate);
        person.Created = NormalizeDateTime(person.Created);
        person.Modified = NormalizeDateTime(person.Modified);
        return person;
    }

    /// <summary>
    /// แปลง DateTime เดี่ยวให้เป็น UTC
    /// </summary>
    private DateTime? NormalizeDateTime(DateTime? dateTime)
    {
        if (!dateTime.HasValue) return null;

        var dt = dateTime.Value;

        if (dt.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        if (dt.Kind == DateTimeKind.Local)
        {
            return dt.ToUniversalTime();
        }

        return dt;
    }

    public TableStorageService(ILogger<TableStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? "UseDevelopmentStorage=true";

        var sourceTableName = Environment.GetEnvironmentVariable("SourceTableName") ?? "caecombinesalestable";
        var targetTableName = Environment.GetEnvironmentVariable("TableName") ?? "mergentbowitherp";
        var matchHistoryTableName = Environment.GetEnvironmentVariable("MatchHistoryTableName") ?? "matchhistory";

        _customerTableClient = new TableClient(_connectionString, sourceTableName);
        _personDocumentTableClient = new TableClient(_connectionString, targetTableName);
        _matchTableClient = new TableClient(_connectionString, matchHistoryTableName);

        // Ensure tables exist
        _ = _customerTableClient.CreateIfNotExistsAsync();
        _ = _personDocumentTableClient.CreateIfNotExistsAsync();
        _ = _matchTableClient.CreateIfNotExistsAsync();
    }

    // CustomerDataEntity methods (source data)
    public async Task<List<CustomerDataEntity>> GetAllCustomersAsync()
    {
        try
        {
            _logger.LogInformation("Getting all customers from source table");

            var customers = new List<CustomerDataEntity>();
            await foreach (var customer in _customerTableClient.QueryAsync<CustomerDataEntity>())
            {
                customers.Add(customer);
            }

            _logger.LogInformation("Retrieved {Count} customers", customers.Count);
            return customers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all customers");
            throw;
        }
    }

    public async Task<CustomerDataEntity?> GetCustomerByIdAsync(string id)
    {
        try
        {
            var response = await _customerTableClient.GetEntityIfExistsAsync<CustomerDataEntity>("Customer", id);
            return response.HasValue ? response.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by ID: {CustomerId}", id);
            throw;
        }
    }

    // PersonDocument methods (merged data)
    public async Task<List<PersonDocument>> GetAllPersonDocumentsAsync()
    {
        try
        {
            _logger.LogInformation("Getting all person documents from merged table");

            var documents = new List<PersonDocument>();
            await foreach (var doc in _personDocumentTableClient.QueryAsync<PersonDocument>())
            {
                documents.Add(doc);
            }

            _logger.LogInformation("Retrieved {Count} person documents", documents.Count);
            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all person documents");
            throw;
        }
    }

    public async Task<PersonDocument?> GetPersonDocumentAsync(string id)
    {
        try
        {
            var response = await _personDocumentTableClient.GetEntityIfExistsAsync<PersonDocument>("MergedCustomer", id);
            return response.HasValue ? response.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting person document by ID: {Id}", id);
            throw;
        }
    }

    public async Task<List<PersonDocument>> SearchPersonDocumentsAsync(SearchCriteria criteria)
    {
        try
        {
            var documents = await GetAllPersonDocumentsAsync();
            var query = documents.AsQueryable();

            if (!string.IsNullOrEmpty(criteria.Query))
            {
                var searchTerm = criteria.Query.ToLowerInvariant();
                query = query.Where(d =>
                    d.CustShortDimName.ToLowerInvariant().Contains(searchTerm) ||
                    d.ProdChipNameDimName.ToLowerInvariant().Contains(searchTerm) ||
                    d.SalespersonDimName.ToLowerInvariant().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(criteria.Country))
            {
                query = query.Where(d => d.RegionDimName3.Equals(criteria.Country, StringComparison.OrdinalIgnoreCase));
            }

            var result = query.Skip((criteria.PageNumber - 1) * criteria.PageSize)
                            .Take(criteria.PageSize)
                            .ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching person documents");
            throw;
        }
    }

    public async Task<PersonDocument> CreatePersonDocumentAsync(PersonDocument person)
    {
        try
        {
            if (string.IsNullOrEmpty(person.RowKey))
            {
                person.RowKey = Guid.NewGuid().ToString();
            }
            person.PartitionKey = "MergedCustomer";
            person.Created = DateTime.UtcNow;
            person.Modified = DateTime.UtcNow;

            await _personDocumentTableClient.AddEntityAsync(person);
            return person;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating person document");
            throw;
        }
    }

    public async Task<PersonDocument> UpdatePersonDocumentAsync(PersonDocument person)
    {
        try
        {
            person.Modified = DateTime.UtcNow;
            await _personDocumentTableClient.UpsertEntityAsync(person, TableUpdateMode.Replace);
            return person;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating person document");
            throw;
        }
    }

    public async Task<bool> DeletePersonDocumentAsync(string id)
    {
        try
        {
            await _personDocumentTableClient.DeleteEntityAsync("MergedCustomer", id);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting person document: {Id}", id);
            throw;
        }
    }

    public async Task<PersonDocument> CreatePersonDocumentFromCustomerAsync(CustomerDataEntity customer)
    {
        try
        {
            var person = customer.ToPersonDocument();
            return await CreatePersonDocumentAsync(person);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating person document from customer");
            throw;
        }
    }


    public async Task<List<PersonDocument>> BulkUpdatePersonDocumentsAsync(List<PersonDocument> persons)
    {
        try
        {
            var updatedPersons = new List<PersonDocument>();

            foreach (var person in persons)
            {
                person.Modified = DateTime.UtcNow;
                if (string.IsNullOrEmpty(person.RowKey))
                {
                    person.RowKey = Guid.NewGuid().ToString();
                    person.Created = DateTime.UtcNow;
                    person.PartitionKey = "MergedCustomer";
                }

                await _personDocumentTableClient.UpsertEntityAsync(person, TableUpdateMode.Replace);
                updatedPersons.Add(person);
            }

            return updatedPersons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk update person documents");
            throw;
        }
    }


    public async Task<List<PersonDocument>> GetRecentlyModifiedRecordsAsync(int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var documents = new List<PersonDocument>();

            await foreach (var doc in _personDocumentTableClient.QueryAsync<PersonDocument>())
            {
                if (doc.Modified >= cutoffDate)
                {
                    documents.Add(doc);
                }
            }

            return documents.OrderByDescending(d => d.Modified).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently modified records");
            throw;
        }
    }

    // Other existing methods remain the same...
    public async Task<CustomerDataEntity> CreateCustomerAsync(CustomerDataEntity customer)
    {
        try
        {
            customer.RowKey = Guid.NewGuid().ToString();
            customer.PartitionKey = "FromSQL";
            customer.Created = DateTime.UtcNow;
            customer.Modified = DateTime.UtcNow;

            await _customerTableClient.AddEntityAsync(customer);
            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            throw;
        }
    }

    public async Task<CustomerDataEntity> UpdateCustomerAsync(CustomerDataEntity customer)
    {
        try
        {
            customer.Modified = DateTime.UtcNow;
            await _customerTableClient.UpsertEntityAsync(customer, TableUpdateMode.Replace);
            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer");
            throw;
        }
    }

    public async Task<bool> DeleteCustomerAsync(string id)
    {
        try
        {
            await _customerTableClient.DeleteEntityAsync("Customer", id);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer");
            throw;
        }
    }

    public async Task<List<CustomerDataEntity>> SearchCustomersAsync(SearchCriteria criteria)
    {
        try
        {
            var customers = await GetAllCustomersAsync();
            var query = customers.AsQueryable();

            if (!string.IsNullOrEmpty(criteria.Query))
            {
                var searchTerm = criteria.Query.ToLowerInvariant();
                query = query.Where(c =>
                    c.CustShortDimName.ToLowerInvariant().Contains(searchTerm) ||
                    c.CustShortDimName.ToLowerInvariant().Contains(searchTerm));
            }

            return query.Skip((criteria.PageNumber - 1) * criteria.PageSize)
                       .Take(criteria.PageSize)
                       .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            throw;
        }
    }

    public async Task<List<CustomerDataEntity>> BulkUpdateCustomersAsync(List<CustomerDataEntity> customers)
    {
        try
        {
            var updatedCustomers = new List<CustomerDataEntity>();

            foreach (var customer in customers)
            {
                customer.Modified = DateTime.UtcNow;
                if (string.IsNullOrEmpty(customer.RowKey))
                {
                    customer.RowKey = Guid.NewGuid().ToString();
                    customer.Created = DateTime.UtcNow;
                    customer.PartitionKey = "Customer";
                }

                await _customerTableClient.UpsertEntityAsync(customer, TableUpdateMode.Replace);
                updatedCustomers.Add(customer);
            }

            return updatedCustomers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk update customers");
            throw;
        }
    }

    // Match operations
    public async Task<List<MatchRecord>> GetMatchHistoryAsync()
    {
        try
        {
            var matches = new List<MatchRecord>();
            await foreach (var match in _matchTableClient.QueryAsync<MatchRecord>())
            {
                matches.Add(match);
            }
            return matches.OrderByDescending(m => m.CreatedDate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match history");
            throw;
        }
    }

    public async Task<MatchRecord> SaveMatchAsync(MatchedRecord matchedRecord)
    {
        try
        {
            var matchRecord = new MatchRecord
            {
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = "MatchRecord",
                SourceRecordId = matchedRecord.SourceRecord?.RowKey ?? "",
                TargetRecordId = matchedRecord.TargetRecord?.Id ?? "",
                SimilarityScore = matchedRecord.SimilarityScore,
                MatchType = matchedRecord.MatchType,
                Confidence = matchedRecord.Confidence,
                Status = matchedRecord.Status,
                CreatedDate = matchedRecord.CreatedDate,
                CreatedBy = matchedRecord.CreatedBy,
                Notes = matchedRecord.Notes
            };

            // Handle JSON serialization safely
            matchRecord.SetMetadata(matchedRecord.Metadata ?? new Dictionary<string, object>());

            await _matchTableClient.AddEntityAsync(matchRecord);
            return matchRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving match record");
            throw;
        }
    }

    public async Task<List<MatchRecord>> GetMatchesByStatusAsync(string status)
    {
        try
        {
            var matches = new List<MatchRecord>();
            await foreach (var match in _matchTableClient.QueryAsync<MatchRecord>())
            {
                if (match.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(match);
                }
            }
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matches by status");
            throw;
        }
    }

    public async Task<MatchRecord> UpdateMatchStatusAsync(string matchId, string status, string approvedBy = "")
    {
        try
        {
            var match = await _matchTableClient.GetEntityAsync<MatchRecord>("MatchRecord", matchId);
            match.Value.Status = status;

            if (!string.IsNullOrEmpty(approvedBy))
            {
                match.Value.ApprovedBy = approvedBy;
                match.Value.ApprovedDate = DateTime.UtcNow;
            }

            await _matchTableClient.UpdateEntityAsync(match.Value, match.Value.ETag, TableUpdateMode.Replace);
            return match.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating match status");
            throw;
        }
    }

    public async Task<DataStatistics> GetDataStatisticsAsync()
    {
        try
        {
            var customers = await GetAllCustomersAsync();
            var personDocuments = await GetAllPersonDocumentsAsync();
            var matches = await GetMatchHistoryAsync();

            return new DataStatistics
            {
                TotalCustomers = personDocuments.Count, // ใช้ merged data แทน
                // ActiveCustomers = personDocuments.Count(c => c.Status == "Active"),
                // InactiveCustomers = personDocuments.Count(c => c.Status == "Inactive"),
                TotalMatches = matches.Count,
                PendingMatches = matches.Count(m => m.Status == "Pending"),
                ApprovedMatches = matches.Count(m => m.Status == "Approved"),
                RejectedMatches = matches.Count(m => m.Status == "Rejected"),
                RecentCustomers = personDocuments.Count(c => c.Created >= DateTime.UtcNow.AddDays(-30)),
                RecentMatches = matches.Count(m => m.CreatedDate >= DateTime.UtcNow.AddDays(-30)),
                AverageSimilarityScore = matches.Any() ? matches.Average(m => m.SimilarityScore) : 0,
                TopRegion = personDocuments.GroupBy(c => c.RegionDimName3)
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopIndustries = personDocuments.GroupBy(c => c.CustAppDimName)
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .ToDictionary(g => g.Key, g => g.Count()),
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics");
            throw;
        }
    }


    public async Task<List<CustomerDataEntity>> GetRecentCustomersAsync(int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var customers = new List<CustomerDataEntity>();

            await foreach (var customer in _customerTableClient.QueryAsync<CustomerDataEntity>())
            {
                if (customer.Created >= cutoffDate)
                {
                    customers.Add(customer);
                }
            }

            return customers.OrderByDescending(c => c.Created).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent customers");
            throw;
        }
    }

    public async Task<List<MatchRecord>> GetRecentMatchesAsync(int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var matches = new List<MatchRecord>();

            await foreach (var match in _matchTableClient.QueryAsync<MatchRecord>())
            {
                if (match.CreatedDate >= cutoffDate)
                {
                    matches.Add(match);
                }
            }

            return matches.OrderByDescending(m => m.CreatedDate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent matches");
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            _ = await _customerTableClient.GetEntityIfExistsAsync<CustomerDataEntity>("Test", "Test");
            _ = await _personDocumentTableClient.GetEntityIfExistsAsync<PersonDocument>("Test", "Test");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetHealthDetailsAsync()
    {
        var details = new Dictionary<string, object>();

        try
        {
            var customers = await GetAllCustomersAsync();
            details["sourceTableRecords"] = customers.Count;

            var personDocs = await GetAllPersonDocumentsAsync();
            details["mergedTableRecords"] = personDocs.Count;

            var matches = await GetMatchHistoryAsync();
            details["matchHistoryRecords"] = matches.Count;

            details["status"] = "Healthy";
        }
        catch (Exception ex)
        {
            details["status"] = "Unhealthy";
            details["error"] = ex.Message;
        }

        return details;
    }

    public async Task<bool> CreateBackupAsync(string backupName)
    {
        try
        {
            _logger.LogInformation("Creating backup: {BackupName}", backupName);

            // Create backup table
            var backupTableName = $"backup_{backupName}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var backupClient = new Azure.Data.Tables.TableClient(_connectionString, backupTableName);

            await backupClient.CreateIfNotExistsAsync();

            // Copy customer data
            var customers = await GetAllCustomersAsync();
            foreach (var customer in customers)
            {
                await backupClient.AddEntityAsync(customer);
            }

            // Copy person documents
            var persons = await GetAllPersonDocumentsAsync();
            foreach (var person in persons)
            {
                await backupClient.AddEntityAsync(person);
            }

            _logger.LogInformation("Backup created successfully: {BackupTableName}", backupTableName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup: {BackupName}", backupName);
            return false;
        }
    }


}