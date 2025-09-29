using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DataMatchBackend.Services;
using Microsoft.Extensions.DependencyInjection;
using DataMatchBackend.Models;
using System.Net;
using System.Text.Json;
using Sic.Login;
using System.Linq;
using Microsoft.Graph.Models;

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


                var result = await ProcessBulkCreate(updateRequest.Records);

                var responseData = new
                {
                    totalCount = updateRequest.Records.Count,
                    successCount = result.SuccessCount,
                    failedCount = result.FailedCount,
                    createdRecords = result.CreatedCustomers,
                    errors = result.ValidationErrors
                };

                return await CreateOkResponse(req, responseData, $"Bulk create completed. {result.SuccessCount} succeeded, {result.FailedCount} failed");
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

                customer.RowKey = id;

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
            return await UnmatchData(req, id);
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
                    var validationResult = _validationService.ValidateBulkUpdate(updateRequest);
                    if (!validationResult.IsValid)
                        return await CreateErrorResponse(req, HttpStatusCode.BadRequest, string.Join(", ", validationResult.Errors));
                }

                if (updateRequest.CreateBackup)
                {
                    var backupName = $"merged_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                    await _dataService!.CreateBackupAsync(backupName);
                    _logger.LogInformation("Created backup: {backupName}", backupName);
                }

                var updatedRecords = await _dataService!.BulkUpdatePersonDocumentsAsync(updateRequest.Records);
                var successCount = updatedRecords.Count;
                var totalCount = updateRequest.Records.Count;

                _logger.LogInformation("Bulk update completed: {SuccessCount}/{TotalCount} successful", successCount, totalCount);

                var data = new { successCount, totalCount, failedCount = totalCount - successCount };
                return await CreateOkResponse(req, data, $"Bulk update completed: {successCount}/{totalCount} successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk update");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Bulk update failed");
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

            // เพิ่ม options เพื่อให้ deserialize ได้ทั้ง camelCase และ PascalCase
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,  // ✅ สำคัญมาก!
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<BulkUpdateRequest>(requestBody, options);
        }

        private async Task<PersonDocument?> ParsePersonDocument(HttpRequestData req)
        {
            var requestBody = await req.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PersonDocument>(requestBody ?? "{}");
        }

        private async Task<(int SuccessCount, int FailedCount, List<PersonDocument> CreatedCustomers, List<string> ValidationErrors)> ProcessBulkCreate(List<PersonDocument> customersToCreate)
        {
            var createdCustomers = new List<PersonDocument>();
            var validationErrors = new List<string>();
            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation("Received {Count} customer documents to process", customersToCreate.Count);

            foreach (var customer in customersToCreate)
            {
                try
                {
                    var filteredCustomer = FilterDataForSave(customer);

                    if (!ShouldSaveCustomer(filteredCustomer))
                    {
                        _logger.LogInformation("Skipping customer {CustomerName} - does not meet save criteria", customer.SelltoCustName_SalesHeader);
                        continue;
                    }

                    // if (_validationService != null)
                    // {
                    //     var customerValidationResult = _validationService.ValidatePersonDocument(filteredCustomer);

                    //     if (!customerValidationResult.IsValid)
                    //     {
                    //         var errorMsg = $"Validation failed for '{filteredCustomer.SelltoCustName_SalesHeader}': {string.Join(", ", customerValidationResult.Errors)}";
                    //         _logger.LogWarning(errorMsg);
                    //         validationErrors.Add(errorMsg);
                    //         failedCount++;
                    //         continue;
                    //     }
                    // }
                    var upsertedCustomer = await _dataService!.UpsertPersonDocumentAsync(filteredCustomer);

                    createdCustomers.Add(upsertedCustomer);
                    successCount++;

                    _logger.LogInformation("Successfully upserted customer: {CustomerName}", filteredCustomer.SelltoCustName_SalesHeader);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upsert individual customer record: {CustomerName}", customer.SelltoCustName_SalesHeader);
                    failedCount++;
                    validationErrors.Add($"Failed to upsert '{customer.SelltoCustName_SalesHeader}': {ex.Message}");
                }
            }

            _logger.LogInformation("Bulk upsert finished. Success: {SuccessCount}, Failed: {FailedCount}", successCount, failedCount);
            return (successCount, failedCount, createdCustomers, validationErrors);
        }
        /// <summary>
        /// กรองข้อมูลที่จะ save เฉพาะ field ที่ต้องการ
        /// </summary>
        private PersonDocument FilterDataForSave(PersonDocument customer)
        //, SharePointContact sharePointContact
        {
            var filtered = new PersonDocument
            {

                PartitionKey = customer.PartitionKey,
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
                Created = customer.Created,
                Modified = DateTime.UtcNow
            };

            return filtered;
        }

        /// <summary>
        /// ตรวจสอบว่าควร save customer นี้หรือไม่
        /// </summary>
        private bool ShouldSaveCustomer(PersonDocument customer)
        {



            // ต้องมี CustShortDimName
            if (string.IsNullOrEmpty(customer.sellToCustomerNo))
                return false;

            // ตัวอย่าง: save เฉพาะ customer ที่มี CustAppDim
            if (string.IsNullOrEmpty(customer.sellToCustomerNo))
                return false;


            return true;
        }


        private async Task<HttpResponseData> UnmatchData(HttpRequestData req, string id)
        {
            try
            {
                _logger.LogInformation("Unmatching/Deleting customer: {CustomerId}", id);

                if (!IsServiceAvailable<IDataService>())
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);

                var existingCustomer = await _dataService!.GetPersonDocumentAsync(id);
                if (existingCustomer == null)
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Customer not found");

                await _dataService.DeletePersonDocumentAsync(id);
                _logger.LogInformation("Deleted/Unmatched customer: {CustomerName} with ID: {CustomerId}", existingCustomer.CustShortDimName, id);

                return await CreateOkResponse<object>(req, null, "Customer deleted/unmatched successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {CustomerId}", id);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to delete customer");
            }
        }

        #endregion
    }
}