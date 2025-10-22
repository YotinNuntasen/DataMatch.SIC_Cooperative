// CustomerDataFunctions.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DataMatchBackend.Services;
using Microsoft.Extensions.DependencyInjection;
using DataMatchBackend.Models;
using System.Net;
using System.Text.Json;
// using Sic.Login;
// using System.Linq;
// using Microsoft.Graph.Models;

namespace DataMatchBackend.Functions
{
    public class CustomerDataFunctions : BaseFunctionService
    {
        private readonly IDataService? _dataService;
        private readonly IValidationService? _validationService;

        public CustomerDataFunctions(IServiceProvider serviceProvider, ILogger<CustomerDataFunctions> logger)
            : base(logger, serviceProvider)
        {
            _dataService = serviceProvider.GetService<IDataService>();
            _validationService = serviceProvider.GetService<IValidationService>();

            LogServiceStatus();
        }

        #region === Merged Data Functions ===

        [Function("GetMergedCustomerData")]
        public async Task<HttpResponseData> GetMergedCustomerData(

            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer-data/merged")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Processing GetMergedCustomerData request");

                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);

                var personDocuments = await _dataService!.GetAllPersonDocumentsAsync();
                _logger.LogInformation("Retrieved {Count} merged customer records", personDocuments.Count);

                return await CreateOkResponse(req, personDocuments, "Merged customer data retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting merged customer data");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetMergedCustomerById")]
        public async Task<HttpResponseData> GetMergedCustomerById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer-data/merged/{id}")] HttpRequestData req, string id)
        {
            try
            {
                _logger.LogInformation("Getting merged customer by ID: {CustomerId}", id);

                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);

                var customer = await _dataService!.GetPersonDocumentAsync(id);
                if (customer == null)
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Merged customer not found");

                return await CreateOkResponse(req, customer, "Merged customer retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting merged customer by ID: {CustomerId}", id);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("SearchMergedCustomers")]
        public async Task<HttpResponseData> SearchMergedCustomers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer-data/merged/search")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Processing merged customer search request");

                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);

                var searchCriteria = await ParseSearchCriteria(req);
                if (searchCriteria == null)
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid search criteria");

                var customers = await _dataService!.SearchPersonDocumentsAsync(searchCriteria);
                _logger.LogInformation("Search completed: {Count} merged customers found", customers.Count);

                return await CreateOkResponse(req, customers, $"Found {customers.Count} customers matching criteria");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching merged customers");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Search failed");
            }
        }

        [Function("ReplaceMergedCustomers")]
        public async Task<HttpResponseData> ReplaceMergedCustomers(
     [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer-data/merged/replace")] HttpRequestData req)
        {
            _logger.LogInformation("Received request to replace all merged customer data.");

            try
            {
                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);

                var request = await ParseBulkUpdateRequest(req);
                if (request == null || !request.Records.Any())
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is empty or invalid. 'records' array is required.");
                }

                const string targetPartitionKey = "MergedData";

                if (_validationService != null)
                {
                    _logger.LogInformation("Validating {RecordCount} records before replacement for PartitionKey '{TargetPartitionKey}'.", request.Records.Count, targetPartitionKey);
                    foreach (var record in request.Records)
                    {

                        var validationResult = _validationService.ValidatePersonDocument(record);
                        if (!validationResult.IsValid)
                        {
                            var errorMessage = $"Validation failed for record with RowKey '{record.RowKey}': {string.Join(", ", validationResult.Errors)}";
                            _logger.LogWarning(errorMessage);
                            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, errorMessage);
                        }
                    }
                    _logger.LogInformation("All records passed validation.");
                }

                _logger.LogInformation("Executing replace operation for PartitionKey '{TargetPartitionKey}' with {RecordCount} new records.", targetPartitionKey, request.Records.Count);


                var (deletedCount, insertedCount) = await _dataService!.ReplaceAllPersonDocumentsAsync(request.Records, targetPartitionKey);

                var result = new
                {
                    Message = $"Data replacement for PartitionKey '{targetPartitionKey}' completed successfully.",
                    DeletedCount = deletedCount,
                    InsertedCount = insertedCount
                };

                return await CreateOkResponse(req, result, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the replace operation.");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An unexpected error occurred while replacing data.");
            }
        }
        [Function("CreateMergedCustomer")]
        public async Task<HttpResponseData> CreateMergedCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer-data/merged")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Attempting to create new merged customer(s)");

                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);

                var updateRequest = await ParseBulkUpdateRequest(req);
                if (updateRequest?.Records == null || !updateRequest.Records.Any())
                {
                    _logger.LogWarning("Request body is empty or contains no customer data");

                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body must contain a list of customer data inside a 'records' property");
                }


                var result = await ProcessBulkUpsert(updateRequest.Records);

                var responseData = new
                {
                    totalCount = updateRequest.Records.Count,
                    successCount = result.SuccessCount,
                    failedCount = result.FailedCount,
                    createdRecords = result.UpsertedPeopleDocuments, 
                    errors = result.ValidationErrors
                };

                return await CreateOkResponse(req, responseData, $"Bulk upsert completed. {result.SuccessCount} succeeded, {result.FailedCount} failed");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize request body. Check JSON format");
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON format in the request body. Expecting an object like { 'records': [...] }");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating merged customers");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An unexpected error occurred");
            }
        }

        [Function("UpdateMergedCustomer")]
        public async Task<HttpResponseData> UpdateMergedCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customer-data/merged/{id}")] HttpRequestData req, string id)
        {
            try
            {
                _logger.LogInformation("Updating merged customer: {CustomerId}", id);

                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);

                var customer = await ParsePersonDocument(req);
                if (customer == null)
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid customer data");

                if (string.IsNullOrEmpty(customer.RowKey) || customer.RowKey != id)
                {
                    customer.RowKey = id;
                }

                if (_validationService != null)
                {
                    var validationResult = _validationService.ValidatePersonDocument(customer);
                    if (!validationResult.IsValid)
                        return await CreateErrorResponse(req, HttpStatusCode.BadRequest, string.Join(", ", validationResult.Errors));
                }

                var updatedCustomer = await _dataService!.UpdatePersonDocumentAsync(customer);
                _logger.LogInformation("Updated merged customer: {CustomerName} with ID: {CustomerId}", updatedCustomer.CustShortDimName, updatedCustomer.RowKey);

                return await CreateOkResponse(req, updatedCustomer, "Customer updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merged customer: {CustomerId}", id);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to update customer");
            }
        }

        [Function("DeleteMergedCustomer")]
        public async Task<HttpResponseData> DeleteMergedCustomer(

            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customer-data/merged/{id}")] HttpRequestData req, string id)
        {

            return await UnmatchMergedRecord(req, id);
        }

        [Function("BulkUpdateMergedCustomers")]
        public async Task<HttpResponseData> BulkUpdateMergedCustomers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer-data/merged/bulk-update")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Processing bulk update request for merged data");

                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);

                var updateRequest = await ParseBulkUpdateRequest(req);
                if (updateRequest?.Records == null || !updateRequest.Records.Any())
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "No records provided for update");

                if (_validationService != null)
                {
                    
                }

                if (updateRequest.CreateBackup)
                {
                    var backupName = $"merged_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                    
                    if (_dataService == null)
                        return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");
                    await _dataService.CreateBackupAsync(backupName);
                    _logger.LogInformation("Created backup: {backupName}", backupName);
                }

                List<PersonDocument> upsertedRecords = new List<PersonDocument>();
                int successfulUpdates = 0;
                int failedUpdates = 0;
                List<string> errorMessages = new List<string>();

                foreach (var record in updateRequest.Records)
                {
                    try
                    {
                       
                        var filteredRecord = FilterDataForSave(record);
                        var upserted = await _dataService!.UpsertPersonDocumentAsync(filteredRecord);
                        if (upserted != null)
                        {
                            upsertedRecords.Add(upserted);
                            successfulUpdates++;
                        }
                        else
                        {
                            _logger.LogWarning("Bulk Update: Failed to upsert record with RowKey {RowKey}", record.RowKey);
                            failedUpdates++;
                            errorMessages.Add($"Failed to upsert record with RowKey {record.RowKey}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Bulk Update: Error processing record with RowKey {RowKey}", record.RowKey);
                        failedUpdates++;
                        errorMessages.Add($"Error processing record with RowKey {record.RowKey}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Bulk upsert completed: {SuccessCount}/{TotalCount} successful", successfulUpdates, updateRequest.Records.Count);

                var data = new { successCount = successfulUpdates, totalCount = updateRequest.Records.Count, failedCount = failedUpdates, errors = errorMessages };
                return await CreateOkResponse(req, data, $"Bulk upsert completed: {successfulUpdates}/{updateRequest.Records.Count} successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk upsert for merged data");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Bulk upsert failed");
            }
        }

        #endregion

        #region === Source Data Functions ===

        [Function("GetSourceCustomerData")]
        public async Task<HttpResponseData> GetSourceCustomerData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer-data/source")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Processing GetSourceCustomerData request");

                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);

                var customers = await _dataService!.GetAllCustomersAsync();
                _logger.LogInformation("Retrieved {Count} source customer records", customers.Count);

                return await CreateOkResponse(req, customers, "Source customer data retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting source customer data");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        #endregion

        #region === Private Helper Methods ===

        private void LogServiceStatus()
        {
            _logger.LogInformation("CustomerDataFunctions initialized:");
            _logger.LogInformation("  - DataService: {Available}", _dataService != null ? "Available" : "Not Available");
            _logger.LogInformation("  - ValidationService: {Available}", _validationService != null ? "Available" : "Not Available");
        }

        private async Task<SearchCriteria?> ParseSearchCriteria(HttpRequestData req)
        {
            var requestBody = await req.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SearchCriteria>(requestBody ?? "{}");
        }

        private async Task<BulkUpdateRequest?> ParseBulkUpdateRequest(HttpRequestData req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            };

            return JsonSerializer.Deserialize<BulkUpdateRequest>(requestBody, options);
        }

        private async Task<PersonDocument?> ParsePersonDocument(HttpRequestData req)
        {
            var requestBody = await req.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<PersonDocument>(requestBody ?? "{}", options);
        }

        private async Task<(int SuccessCount, int FailedCount, List<PersonDocument> UpsertedPeopleDocuments, List<string> ValidationErrors)> ProcessBulkUpsert(List<PersonDocument> peopleDocumentsToUpsert)
        {
            var upsertedRecords = new List<PersonDocument>();
            var validationErrors = new List<string>();
            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation("Received {Count} person documents to process for upsert", peopleDocumentsToUpsert.Count);

            foreach (var personDocument in peopleDocumentsToUpsert)
            {
                try
                {
                
                    if (string.IsNullOrEmpty(personDocument.PartitionKey) && !string.IsNullOrEmpty(personDocument.OpportunityId))
                    {
                        personDocument.PartitionKey = personDocument.OpportunityId;
                    }
                    if (string.IsNullOrEmpty(personDocument.RowKey))
                    {
                    
                    }

                    var filteredPersonDocument = FilterDataForSave(personDocument);


                    var upserted = await _dataService!.UpsertPersonDocumentAsync(filteredPersonDocument);

                    if (upserted != null)
                    {
                        upsertedRecords.Add(upserted);
                        successCount++;
                        _logger.LogInformation("Successfully upserted person document: {CustShortDimName} (RowKey: {RowKey})", filteredPersonDocument.CustShortDimName, filteredPersonDocument.RowKey);
                    }
                    else
                    {
                        failedCount++;
                        validationErrors.Add($"Failed to upsert '{personDocument.CustShortDimName}' (RowKey: {personDocument.RowKey}): Upsert returned null.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upsert individual person document record: {CustShortDimName} (RowKey: {RowKey})", personDocument.CustShortDimName, personDocument.RowKey);
                    failedCount++;
                    validationErrors.Add($"Failed to upsert '{personDocument.CustShortDimName}' (RowKey: {personDocument.RowKey}): {ex.Message}");
                }
            }

            _logger.LogInformation("Bulk upsert finished. Success: {SuccessCount}, Failed: {FailedCount}", successCount, failedCount);
            return (successCount, failedCount, upsertedRecords, validationErrors);
        }

        /// <summary>
        /// กรองข้อมูลที่จะ save เฉพาะ field ที่ต้องการ
        /// </summary>
        private PersonDocument FilterDataForSave(PersonDocument customer)
        {

            var filtered = new PersonDocument
            {
                PartitionKey = customer.PartitionKey ?? customer.OpportunityId ?? "DefaultPartition",
                RowKey = customer.RowKey,
                OpportunityId = customer.OpportunityId,
                OpportunityName = customer.OpportunityName,
                CustShortDimName = customer.CustShortDimName,
                PrefixdocumentNo = customer.PrefixdocumentNo,
                SelltoCustName_SalesHeader = customer.SelltoCustName_SalesHeader,
                SystemRowVersion = customer.SystemRowVersion,
                DocumentDate = customer.DocumentDate,
                documentNo = customer.documentNo,
                itemReferenceNo = customer.itemReferenceNo,
                lineNo = customer.lineNo,
                no = customer.no,
                quantity = customer.quantity,
                sellToCustomerNo = customer.sellToCustomerNo,
                shipmentNo = customer.shipmentNo,
                sodocumentNo = customer.sodocumentNo,
                unitPrice = customer.unitPrice,
                lineAmount = customer.lineAmount,
                CurrencyRate = customer.CurrencyRate,
                SalesPerUnit = customer.SalesPerUnit,
                TotalSales = customer.TotalSales,
                CustAppDimName = customer.CustAppDimName,
                ProdChipNameDimName = customer.ProdChipNameDimName,
                RegionDimName3 = customer.RegionDimName3,
                SalespersonDimName = customer.SalespersonDimName,
                description = customer.description,
                Created = customer.Created == default ? DateTime.UtcNow : customer.Created,
                Modified = DateTime.UtcNow
            };

            return filtered;
        }

        /// <summary>
        /// ตรวจสอบว่าควร save customer นี้หรือไม่ (เงื่อนไขที่เข้มงวดมากขึ้น)
        /// </summary>
        private bool ShouldSaveCustomer(PersonDocument customer)
        {
    
            if (string.IsNullOrEmpty(customer.OpportunityId) || string.IsNullOrEmpty(customer.RowKey))
            {
                _logger.LogWarning("Skipping save for PersonDocument with missing OpportunityId or RowKey.");
                return false;
            }

            if (string.IsNullOrEmpty(customer.CustShortDimName) && string.IsNullOrEmpty(customer.SelltoCustName_SalesHeader))
            {
                _logger.LogWarning("Skipping save for PersonDocument with no customer name information.");
                return false;
            }

            return true;
        }

        private async Task<HttpResponseData> UnmatchMergedRecord(HttpRequestData req, string azureRowKey)
        {
            try
            {
                _logger.LogInformation("Attempting to unmatch/delete merged record with Azure RowKey: {AzureRowKey}", azureRowKey);

                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                var existingRecord = await _dataService!.GetPersonDocumentAsync(azureRowKey); 
                if (existingRecord == null)
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Merged record with RowKey '{azureRowKey}' not found.");

                var deleted = await _dataService.DeletePersonDocumentAsync(azureRowKey);
                if (!deleted)
                {
                    _logger.LogError("Failed to delete merged record with RowKey: {AzureRowKey}", azureRowKey);
                    return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, $"Failed to delete merged record with RowKey '{azureRowKey}'.");
                }

                _logger.LogInformation("Successfully deleted merged record: {CustShortDimName} (RowKey: {AzureRowKey}) associated with OpportunityId: {OpportunityId}",
                                       existingRecord.CustShortDimName, azureRowKey, existingRecord.OpportunityId);

                return await CreateOkResponse<object>(req, null, $"Merged record (RowKey: {azureRowKey}) deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unmatching/deleting merged record with Azure RowKey: {AzureRowKey}", azureRowKey);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to unmatch/delete merged record.");
            }
        }
        #endregion
    }
}