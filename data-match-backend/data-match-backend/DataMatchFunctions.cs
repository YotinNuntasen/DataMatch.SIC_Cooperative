using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DataMatchBackend.Authentication;
using Microsoft.Extensions.DependencyInjection;
using DataMatchBackend.Models;
using DataMatchBackend.Services;
using System.Net;
using System.Text.Json;
using Sic.Login;

namespace DataMatchBackend.Functions
{
    public class DataMatchFunctions : BaseFunctionService
    {
        private readonly IDataService? _dataService;
        private readonly ISimilarityService? _similarityService;
        private readonly ISharePointService? _sharePointService;

        public DataMatchFunctions(IServiceProvider serviceProvider, ILogger<DataMatchFunctions> logger) 
            : base(logger, serviceProvider)
        {
            _dataService = serviceProvider.GetService<IDataService>();
            _similarityService = serviceProvider.GetService<ISimilarityService>();
            _sharePointService = serviceProvider.GetService<ISharePointService>();

            LogServiceStatus();
        }

        [Function("GetSharePointData")]
        public async Task<HttpResponseData> GetSharePointData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sharepoint-data")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting SharePoint data for matching");

                if (!IsServiceAvailable<ISharePointService>())
                {
                    return await CreateServiceUnavailableResponse(req, "SharePoint", "ENABLE_SHAREPOINT_SERVICE");
                }

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                var sharePointResult = await _sharePointService!.GetOpportunityListAsync(authResult.UserToken ?? "");

                if (!sharePointResult.Success)
                {
                    _logger.LogError("Failed to get SharePoint data: {Message}", sharePointResult.Message);
                    var statusCode = MapSharePointErrorCode(sharePointResult.ErrorCode);
                    return await CreateErrorResponse(req, statusCode, sharePointResult.Message);
                }

                var sharePointContacts = sharePointResult.Data ?? new List<SharePointContact>();
                _logger.LogInformation("Retrieved {Count} SharePoint contacts", sharePointContacts.Count);

                return await CreateOkResponse(req, sharePointContacts, "SharePoint data retrieved successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Authentication failed in GetSharePointData");
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Failed to authenticate with SharePoint on behalf of the user");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SharePoint data");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get SharePoint data");
            }
        }

        [Function("PerformAutoMatchingWorkflow")]
        public async Task<HttpResponseData> PerformAutoMatchingWorkflow(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "data-match/workflow")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Performing automatic data matching and update workflow");

                if (!AreRequiredServicesAvailable())
                {
                    return await CreateErrorResponse(req, HttpStatusCode.ServiceUnavailable, "One or more required services are not available");
                }

                var authResult = await ValidateAuthenticationAsync(req);
                if (!authResult.IsValid)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, authResult.ErrorMessage);
                }

                var result = await ExecuteMatchingWorkflow(authResult.UserToken ?? "");
                var message = $"Workflow completed. Records created: {result.CreatedCount}, Records updated: {result.UpdatedCount}";
                _logger.LogInformation(message);

                return await CreateOkResponse(req, new { result.CreatedCount, result.UpdatedCount }, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in matching workflow");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Matching workflow failed");
            }
        }

        [Function("GetMatchingSuggestions")]
        public async Task<HttpResponseData> GetMatchingSuggestions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "data-match/suggestions")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting matching suggestions");

                if (!IsServiceAvailable<IDataService>() || !IsServiceAvailable<ISimilarityService>())
                {
                    return await CreateErrorResponse(req, HttpStatusCode.ServiceUnavailable, "Required services are not available");
                }

                var suggestionRequest = await ParseSuggestionRequest(req);
                if (suggestionRequest?.SourceRecord == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Source record is required");
                }

                var sourceDoc = JsonSerializer.Deserialize<PersonDocument>(JsonSerializer.Serialize(suggestionRequest.SourceRecord));
                if (sourceDoc == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid source record format");
                }

                var suggestions = await GenerateMatchingSuggestions(sourceDoc);
                _logger.LogInformation("Found {Count} matching suggestions", suggestions.Count);

                return await CreateOkResponse(req, suggestions, $"Found {suggestions.Count} matching suggestions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting matching suggestions");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get matching suggestions");
            }
        }

        [Function("GetMatchHistory")]
        public async Task<HttpResponseData> GetMatchHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "data-match/history")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting match history");

                if (!IsServiceAvailable<IDataService>())
                {
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");
                }

                var matchHistory = await _dataService!.GetMatchHistoryAsync();
                _logger.LogInformation("Retrieved {Count} match records", matchHistory.Count);

                return await CreateOkResponse(req, matchHistory, "Match history retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting match history");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get match history");
            }
        }

        [Function("UpdateMatchStatus")]
        public async Task<HttpResponseData> UpdateMatchStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "data-match/status")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Updating match status");

                if (!IsServiceAvailable<IDataService>())
                {
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");
                }

                var statusRequest = await ParseMatchStatusRequest(req);
                if (string.IsNullOrEmpty(statusRequest?.MatchId) || string.IsNullOrEmpty(statusRequest.Status))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "MatchId and Status are required");
                }

                var updatedMatch = await _dataService!.UpdateMatchStatusAsync(
                    statusRequest.MatchId,
                    statusRequest.Status,
                    statusRequest.ApprovedBy ?? "System"
                );

                _logger.LogInformation("Updated match status for ID: {MatchId} to {Status}", statusRequest.MatchId, statusRequest.Status);

                return await CreateOkResponse(req, updatedMatch, "Match status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating match status");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to update match status");
            }
        }

        [Function("UnmatchData")]
        public async Task<HttpResponseData> UnmatchData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "data-match/unmatch")] HttpRequestData req)
        {
            try
            {
                if (!IsServiceAvailable<IDataService>())
                {
                    return await CreateServiceUnavailableResponse(req, "Data", "ENABLE_DATA_SERVICE");
                }

                var unmatchRequest = await ParseUnmatchRequest(req);
                if (string.IsNullOrEmpty(unmatchRequest?.MergedRecordId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "MergedRecordId is required");
                }

                _logger.LogInformation("Unmatching record with ID: {Id}", unmatchRequest.MergedRecordId);

                var success = await _dataService!.DeletePersonDocumentAsync(unmatchRequest.MergedRecordId);

                if (success)
                {
                    _logger.LogInformation("Successfully unmatched and deleted record ID: {Id}", unmatchRequest.MergedRecordId);
                    return await CreateOkResponse(req, new { deletedId = unmatchRequest.MergedRecordId }, "Record unmatched successfully");
                }
                else
                {
                    _logger.LogWarning("Record ID not found for unmatching: {Id}", unmatchRequest.MergedRecordId);
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Record not found to unmatch");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during unmatch operation");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Unmatch operation failed");
            }
        }

        // Private helper methods
        private void LogServiceStatus()
        {
            _logger.LogInformation("DataMatchFunctions initialized:");
            _logger.LogInformation("  - DataService: {Available}", _dataService != null ? "Available" : "Not Available");
            _logger.LogInformation("  - SimilarityService: {Available}", _similarityService != null ? "Available" : "Not Available");
            _logger.LogInformation("  - SharePointService: {Available}", _sharePointService != null ? "Available" : "Not Available");
        }

        private bool AreRequiredServicesAvailable()
        {
            return _dataService != null && _similarityService != null && _sharePointService != null;
        }

        private HttpStatusCode MapSharePointErrorCode(string? errorCode)
        {
            return errorCode switch
            {
                "SP_UNAUTHORIZED" => HttpStatusCode.Unauthorized,
                "SP_FORBIDDEN" => HttpStatusCode.Forbidden,
                "SP_NOT_FOUND" => HttpStatusCode.NotFound,
                _ => HttpStatusCode.InternalServerError
            };
        }

        private async Task<SuggestionRequest?> ParseSuggestionRequest(HttpRequestData req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            return JsonSerializer.Deserialize<SuggestionRequest>(requestBody ?? "{}");
        }

        private async Task<MatchStatusRequest?> ParseMatchStatusRequest(HttpRequestData req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            return JsonSerializer.Deserialize<MatchStatusRequest>(requestBody ?? "{}");
        }

        private async Task<UnmatchRequest?> ParseUnmatchRequest(HttpRequestData req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            return JsonSerializer.Deserialize<UnmatchRequest>(requestBody ?? "{}");
        }

        private async Task<(int CreatedCount, int UpdatedCount)> ExecuteMatchingWorkflow(string userToken)
        {
            var sharePointResult = await _sharePointService!.GetOpportunityListAsync(userToken);

            if (!sharePointResult.Success)
            {
                _logger.LogError("Failed to load SharePoint data: {Message}", sharePointResult.Message);
                throw new InvalidOperationException(sharePointResult.Message);
            }

            var sharePointContacts = sharePointResult.Data ?? new List<SharePointContact>();
            _logger.LogInformation("Retrieved {Count} SharePoint contacts", sharePointContacts.Count);

            var sourceAzureCustomers = await _dataService!.GetAllCustomersAsync();
            var sourcePersonDocs = sourceAzureCustomers.ToPersonDocuments();
            _logger.LogInformation("Retrieved {Count} source Azure Table records", sourcePersonDocs.Count);

            var matches = _similarityService!.FindBestMatches(sharePointContacts, sourcePersonDocs, 80.0);
            _logger.LogInformation("Auto-matching completed: {MatchCount} matches found", matches.Count);

            var createdCount = 0;
            var updatedCount = 0;

            foreach (var matchedRecord in matches)
            {
                if (matchedRecord.SharePointData == null || matchedRecord.AzureData == null) continue;

                var azureDoc = matchedRecord.AzureData.ToPersonDocument();
                var mergedDoc = azureDoc.MergeWithSharePointData(matchedRecord.SharePointData);

                var existingMergedDoc = await _dataService.GetPersonDocumentAsync(azureDoc.RowKey);

                if (existingMergedDoc != null)
                {
                    mergedDoc.RowKey = existingMergedDoc.RowKey;
                    await _dataService.UpdatePersonDocumentAsync(mergedDoc);
                    updatedCount++;
                }
                else
                {
                    await _dataService.CreatePersonDocumentAsync(mergedDoc);
                    createdCount++;
                }
                await _dataService.SaveMatchAsync(matchedRecord);
            }

            return (createdCount, updatedCount);
        }

        private async Task<List<SuggestionResult>> GenerateMatchingSuggestions(PersonDocument sourceDoc)
        {
            var targetRecords = await _dataService!.GetAllPersonDocumentsAsync();
            var suggestions = new List<SuggestionResult>();

            foreach (var targetRecord in targetRecords)
            {
                if (targetRecord.RowKey == sourceDoc.RowKey) continue;

                var similarity = _similarityService!.CalculateSimilarity(sourceDoc, targetRecord);
                if (similarity >= 60)
                {
                    var confidence = _similarityService.GetConfidenceLevel(similarity);
                    var reasons = GetMatchReasons(sourceDoc, targetRecord, similarity);

                    suggestions.Add(new SuggestionResult
                    {
                        TargetRecord = targetRecord,
                        Similarity = similarity,
                        Confidence = confidence,
                        MatchReasons = reasons
                    });
                }
            }

            return suggestions.OrderByDescending(s => s.Similarity).Take(5).ToList();
        }

        private List<string> GetMatchReasons(PersonDocument record1, PersonDocument record2, double similarity)
        {
            var reasons = new List<string>();

            if (!string.IsNullOrEmpty(record1.CustShortDimName) && !string.IsNullOrEmpty(record2.CustShortDimName))
            {
                if (record1.CustShortDimName.Equals(record2.CustShortDimName, StringComparison.OrdinalIgnoreCase))
                    reasons.Add("Exact customer name match");
                else if (record1.CustShortDimName.Contains(record2.CustShortDimName, StringComparison.OrdinalIgnoreCase) ||
                         record2.CustShortDimName.Contains(record1.CustShortDimName, StringComparison.OrdinalIgnoreCase))
                    reasons.Add("Partial customer name match");
            }
            return reasons;
        }
    }

    // Model Classes
    public class UnmatchRequest
    {
        [JsonPropertyName("mergedRecordId")]
        public string? MergedRecordId { get; set; }
    }

    public class AutoMatchRequest
    {
        [JsonPropertyName("threshold")]
        public double Threshold { get; set; } = 80.0;

        [JsonPropertyName("autoApprove")]
        public bool AutoApprove { get; set; } = false;
    }

    public class SuggestionRequest
    {
        [JsonPropertyName("sourceRecord")]
        public object? SourceRecord { get; set; }

        [JsonPropertyName("maxSuggestions")]
        public int MaxSuggestions { get; set; } = 5;

        [JsonPropertyName("minSimilarity")]
        public double MinSimilarity { get; set; } = 60.0;
    }

    public class SuggestionResult
    {
        [JsonPropertyName("targetRecord")]
        public PersonDocument? TargetRecord { get; set; }

        [JsonPropertyName("similarity")]
        public double Similarity { get; set; }

        [JsonPropertyName("confidence")]
        public string Confidence { get; set; } = "";

        [JsonPropertyName("matchReasons")]
        public List<string> MatchReasons { get; set; } = new();
    }

    public class MatchStatusRequest
    {
        [JsonPropertyName("matchId")]
        public string? MatchId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("approvedBy")]
        public string? ApprovedBy { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}