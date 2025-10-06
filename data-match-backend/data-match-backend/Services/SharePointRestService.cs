using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataMatchBackend.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Net.Http;

namespace DataMatchBackend.Services
{
    public class SharePointRestService : ISharePointService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SharePointRestService> _logger;
        private readonly string _listTitle; 
        private readonly string _customerListTitle; 

        
        private record OpportunityRawData(
            JsonElement Json, // เก็บ JsonElement ทั้งก้อนไว้ก่อน
            int CustomerId // ดึง CustomerId ออกมาเลย
        );

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public SharePointRestService(HttpClient httpClient, ILogger<SharePointRestService> logger, IOptions<SharePointServiceOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var spOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _listTitle = spOptions.OpportunityListTitle ?? throw new InvalidOperationException("OpportunityListTitle is required");
            _customerListTitle = "Customer List"; // กำหนดชื่อ Customer List ที่ถูกต้อง

            // ไม่จำเป็นต้องใช้ _salePersonListTitle ใน Constructor นี้แล้ว
            // เพราะ Sale Person Field ที่เราต้องการอยู่ใน Customer List
        }

        public async Task<SharePointApiResponse<List<SharePointContact>>> GetOpportunityListAsync(string userToken)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Getting all SharePoint opportunities from list '{listTitle}'.", _listTitle);

            if (string.IsNullOrEmpty(userToken))
            {
                return SharePointApiResponse<List<SharePointContact>>.Error(
                    "User access token is required for SharePoint access", "SP_NO_TOKEN");
            }

            try
            {
                var selectFields = new List<string>
                {
                    "Id", "Opportunity_x0020_ID", "Title", "Filter_x0020_Tag",
                    "Expected_x0020_Revenue", "Priority", "Pipeline_x0020_Stage",
                    "Product_x0020_Group", "Register_x0020_Date", "Note",
                    "Next_x0020_Action_x0020_Date", "Suspend",
                    "Suspend_x0020_reason", "Step2_x002d_Entry_x002d_Date", "Step3_x002d_Entry_x002d_Date",
                    "Step4_x002d_Entry_x002d_Date", "Step5_x002d_Entry_x002d_Date", "S6_x002d_Eval_x002d_Entry_x002d_",
                    "S7_x002d_DI_x002d_Entry_x002d_Da", "S8_x002d_PrePro_x002d_Entry_x002", 
                    "S9_x002d_DWIN_x002d_Entry_x002d_",
                    "Source_x0020_of_x0020_Lead", "MassProductionDate", "Closed_x002d_Lost",
                    "Closed_x002d_Lost_x0020_Reason", "Closed_x002d_LostDate", "Closed_x002d_LostCommonReason",
                    "Expected_x0020_Volume", "Target_x0020_Price", "Cal_x0020_Expected_x0020_Revenue",
                    "MultilineComment", "ProductName", "ProductCode", "ActivityStatus", "DailyLatestComment",
                    "ActionOwner", "Modified", "Created",

                    // Fields จาก Lookup ชั้นแรก (สำคัญมาก)
                    "Author/Title",
                    "Editor/Title",
                    "Customer_x0020_Name/Title",  // ดึง Title ของ Customer
                    "Customer_x0020_Name/Id",     // <<< สำคัญ: ดึง ID ของ Customer มาด้วย
                    "Distributor/Title",
                    "CAE_x0020_in_x002d_charge/Title",
                };

                var expandFields = new List<string>
                {
                    "Author",
                    "Editor",
                    "Customer_x0020_Name",
                    "Distributor",
                    "CAE_x0020_in_x002d_charge",
                };

                var selectQuery = string.Join(",", selectFields);
                var expandQuery = string.Join(",", expandFields);
                var endpoint = $"_api/web/lists/getbytitle('{_listTitle}')/items?$select={selectQuery}&$expand={expandQuery}&$top=500";

                var rawOpportunities = await GetItemsFromSharePointAsync(endpoint, userToken, item =>
                    new OpportunityRawData(
                        item.Clone(), // Clone JsonElement ตรงนี้เพื่อใช้ Map ในภายหลัง
                        GetInt(item, "Customer_x0020_Name", "Id") // ดึง Customer Id จาก Opportunity List
                    )
                );

                var customerIds = rawOpportunities
                    .Select(opp => opp.CustomerId)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                // ดึง Sale Person Name โดยใช้ Customer Ids ที่รวบรวมได้
                var salePersonNameByCustomerId = await GetSalePersonNamesByCustomerIdsAsync(userToken, customerIds);

                var contacts = rawOpportunities
                    .Select(oppRaw => MapListItemToSharePointContact(oppRaw.Json, salePersonNameByCustomerId))
                    .ToList();

                stopwatch.Stop();
                _logger.LogInformation("Successfully retrieved {Count} contacts from SharePoint in {ElapsedMs}ms.",
                    contacts.Count, stopwatch.ElapsedMilliseconds);

                return SharePointApiResponse<List<SharePointContact>>.Ok(contacts,
                    $"Retrieved {contacts.Count} contacts successfully")
                    .WithUserContext(userToken)
                    .WithMetadata("responseTime", stopwatch.ElapsedMilliseconds)
                    .WithMetadata("itemCount", contacts.Count)
                    .WithMetadata("listName", _listTitle);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Error accessing SharePoint: {StatusCode}", ex.StatusCode);
                
                var statusCode = ex.StatusCode;
                var errorCode = "SP_HTTP_ERROR";
                string message = "SharePoint access failed.";

                if (statusCode == System.Net.HttpStatusCode.Unauthorized || statusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    errorCode = "SP_UNAUTHORIZED";
                    message = "Access denied or token expired. Please re-login.";
                }
                else if (statusCode == System.Net.HttpStatusCode.NotFound)
                {
                    errorCode = "SP_NOT_FOUND";
                    message = "SharePoint list was not found.";
                }

                return SharePointApiResponse<List<SharePointContact>>.Error(message, errorCode);
            }
           
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error accessing SharePoint with User Context");
                return SharePointApiResponse<List<SharePointContact>>.Error(
                    "An unexpected error occurred while accessing SharePoint.",
                    "SP_UNEXPECTED_ERROR");
            }
        }

        /// <summary>
        /// ดึงชื่อ Sale Person (User Field) จาก Customer List โดยใช้ Customer IDs
        /// </summary>
        private async Task<Dictionary<int, string>> GetSalePersonNamesByCustomerIdsAsync(string userToken, IEnumerable<int> customerIds)
        {
            if (!customerIds.Any())
            {
                return new Dictionary<int, string>();
            }

            var uniqueCustomerIds = customerIds.Distinct().ToHashSet();

            var allSalePersonNames = new Dictionary<int, string>();
            string salePersonLookupUserField = "Sale_x0020_Person";
            
            var endpoint = $"_api/web/lists/getbytitle('{_customerListTitle}')/items?" +
                           $"$select=Id,{salePersonLookupUserField}/Title&" +
                           $"$expand={salePersonLookupUserField}";

            _logger.LogInformation("Querying ALL Customer data for Sale Person with endpoint: {endpoint}", endpoint);

            try
            {
                var allCustomerData = await GetItemsFromSharePointAsync(endpoint, userToken, (item) => new
                {
                    CustomerId = GetInt(item, "Id"),
                    SalePersonName = GetStringFromNestedLookup(item, salePersonLookupUserField, "Title")
                });

                
                foreach (var customerItem in allCustomerData)
                {
                    if (uniqueCustomerIds.Contains(customerItem.CustomerId) &&
                        customerItem.CustomerId > 0 &&
                        !string.IsNullOrEmpty(customerItem.SalePersonName))
                    {
                        allSalePersonNames[customerItem.CustomerId] = customerItem.SalePersonName;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error getting ALL customer data. Endpoint: {endpoint}. Error: {message}", endpoint, ex.Message);
                throw;
            }

            return allSalePersonNames;
        }
               private string GetStringFromNestedLookup(JsonElement element, string key, string subKey)
        {
            if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Object)
            {
                
                if (prop.TryGetProperty(subKey, out var subProp) && subProp.ValueKind != JsonValueKind.Null)
                {
                    return subProp.GetString() ?? "";
                }
               
            }
            return "";
        }

        private SharePointContact MapListItemToSharePointContact(JsonElement item, Dictionary<int, string> salePersonNameByCustomerId)
        {

            string GetBaseLookupTitle(JsonElement element, string key)
            {
                if (element.TryGetProperty(key, out var prop) && prop.ValueKind != JsonValueKind.Null)
                {

                    if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("Title", out var titleProp))
                    {
                        return titleProp.GetString() ?? "";
                    }

                    return GetString(prop, "Title");
                }
                return "";
            }

            int GetBaseLookupId(JsonElement element, string key)
            {
                if (element.TryGetProperty(key, out var prop) && prop.ValueKind != JsonValueKind.Null)
                {
                    if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("Id", out var idProp))
                    {
                        if (idProp.TryGetInt32(out int resultFromObject)) return resultFromObject;
                    }
                    else if (prop.TryGetInt32(out int resultDirect))
                    {
                        return resultDirect;
                    }
                }
                return 0;
            }

            var customerId = GetBaseLookupId(item, "Customer_x0020_Name");

            var salePersonName = "";
            if (customerId > 0 && salePersonNameByCustomerId.TryGetValue(customerId, out var name))
            {
                salePersonName = name;
            }

            return new SharePointContact
            {
                // --- Fields หลัก ---
                Id = GetString(item, "Id"),
                opportunityId = GetString(item, "Opportunity_x0020_ID"),
                opportunityName = GetString(item, "Title"),
                FilterTag = GetString(item, "Filter_x0020_Tag"),
                ExpectedRevenueUSD = GetDecimal(item, "Expected_x0020_Revenue"),
                Priority = GetString(item, "Priority"),
                PipelineStage = GetString(item, "Pipeline_x0020_Stage"),
                ProductGroup = GetString(item, "Product_x0020_Group"),
                RegisterDate = GetDateTime(item, "Register_x0020_Date"),
                Note = GetString(item, "Note"),
                NextActionDate = GetDateTime(item, "Next_x0020_Action_x0020_Date"),

                IsSuspended = GetBoolean(item, "Suspend"),
                SuspendReason = GetString(item, "Suspend_x0020_reason"),
                SourceOfLead = GetString(item, "Source_x0020_of_x0020_Lead"),
                ProductName = GetString(item, "ProductName"),
                ProductCode = GetString(item, "ProductCode"),
                ActivityStatus = GetString(item, "ActivityStatus"),
                ActionOwner = GetString(item, "ActionOwner"),
                DailyLatestComment = GetString(item, "DailyLatestComment"),
                MultilineComment = GetString(item, "MultilineComment"),
                MassProductionDate = GetDateTime(item, "MassProductionDate"),
                ExpectedVolume = GetDecimal(item, "Expected_x0020_Volume"),
                TargetPrice = GetDecimal(item, "Target_x0020_Price"),
                CalculatedExpectedRevenue = GetDecimal(item, "Cal_x0020_Expected_x0020_Revenue"),

                // --- วันที่ในแต่ละ Stage ---
                Step2EntryDate = GetDateTime(item, "Step2_x002d_Entry_x002d_Date"),
                Step3EntryDate = GetDateTime(item, "Step3_x002d_Entry_x002d_Date"),
                Step4EntryDate = GetDateTime(item, "Step4_x002d_Entry_x002d_Date"),
                Step5EntryDate = GetDateTime(item, "Step5_x002d_Entry_x002d_Date"),
                S6EvalEntryDate = GetDateTime(item, "S6_x002d_Eval_x002d_Entry_x002d_"),
                S7DIEntryDate = GetDateTime(item, "S7_x002d_DI_x002d_Entry_x002d_Da"),
                S8PreProEntryDate = GetDateTime(item, "S8_x002d_PrePro_x002d_Entry_x002"),
                S9DWINEntryDate = GetDateTime(item, "S9_x002d_DWIN_x002d_Entry_x002d_"),

                // --- ข้อมูลสถานะ Closed-Lost ---
                IsClosedLost = GetBoolean(item, "Closed_x002d_Lost"),
                ClosedLostReason = GetString(item, "Closed_x002d_Lost_x0020_Reason"),
                ClosedLostDate = GetDateTime(item, "Closed_x002d_LostDate"),
                ClosedLostCommonReason = GetString(item, "Closed_x002d_LostCommonReason"),


                CustomerName = GetBaseLookupTitle(item, "Customer_x0020_Name"),
                DistributorName = GetBaseLookupTitle(item, "Distributor"),
                CaeInCharge = GetBaseLookupTitle(item, "CAE_x0020_in_x002d_charxge"),
                CreatedBy = GetBaseLookupTitle(item, "Author"),
                ModifiedBy = GetBaseLookupTitle(item, "Editor"),
                CustomerNameSalePersonCode = salePersonName,
            };
        }


        public async Task<SharePointApiResponse<SharePointContact?>> GetOpportunityByIdAsync(string userToken, string id)
        {
            _logger.LogInformation("Getting SharePoint opportunity by ID '{itemId}' using User Context.", id);

            if (string.IsNullOrEmpty(userToken))
            {
                return SharePointApiResponse<SharePointContact?>.Error(
                    "User access token is required", "SP_NO_TOKEN");
            }

            try
            {

                var selectFields = new List<string>
                {
                    // ใส่ฟิลด์ที่จำเป็นทั้งหมดที่นี่
                    "Id", "Opportunity_x0020_ID", "Title", "Filter_x0020_Tag",
                    "Expected_x0020_Revenue", "Priority", "Pipeline_x0020_Stage",
                    "Product_x0020_Group", "Register_x0020_Date", "Note",
                    "Next_x0020_Action_x0020_Date", "Suspend",
                    "Suspend_x0020_reason", "Step2_x002d_Entry_x002d_Date", "Step3_x002d_Entry_x002d_Date",
                    "Step4_x002d_Entry_x002d_Date", "Step5_x002d_Entry_x002d_Date", "S6_x002d_Eval_x002d_Entry_x002d_",
                    "S7_x002d_DI_x002d_Entry_x002d_Da", "S8_x002d_PrePro_x002d_Entry_x002",
                    "S9_x002d_DWIN_x002d_Entry_x002d_",
                    "Source_x0020_of_x0020_Lead", "MassProductionDate", "Closed_x002d_Lost",
                    "Closed_x002d_Lost_x0020_Reason", "Closed_x002d_LostDate", "Closed_x002d_LostCommonReason",
                    "Expected_x0020_Volume", "Target_x0020_Price", "Cal_x0020_Expected_x0020_Revenue",
                    "MultilineComment", "ProductName", "ProductCode", "ActivityStatus", "DailyLatestComment",
                    "ActionOwner", "Modified", "Created",
                    "Author/Title",
                    "Editor/Title",
                    "Customer_x0020_Name/Title",
                    "Customer_x0020_Name/Id", // สำคัญมาก
                    "Distributor/Title",
                    "CAE_x0020_in_x002d_charge/Title",
                };

                var expandFields = new List<string>
                {
                    "Author", "Editor", "Customer_x0020_Name", "Distributor", "CAE_x0020_in_x002d_charge",
                };

                var selectQuery = string.Join(",", selectFields);
                var expandQuery = string.Join(",", expandFields);
                var endpoint = $"_api/web/lists/getbytitle('{_listTitle}')/items({id})?$select={selectQuery}&$expand={expandQuery}";

                var spItem = await GetSingleItemFromSharePointAsync(endpoint, userToken);

                if (spItem.HasValue)
                {
                    var itemJson = spItem.Value;

                    var customerId = GetInt(itemJson, "Customer_x0020_Name", "Id");
                    var salePersonNameByCustomerId = new Dictionary<int, string>();

                    if (customerId > 0)
                    {
                        // ดึง Sale Person Name สำหรับ Customer ID เดียว
                        salePersonNameByCustomerId = await GetSalePersonNamesByCustomerIdsAsync(userToken, new List<int> { customerId });
                    }

                    var contact = MapListItemToSharePointContact(itemJson, salePersonNameByCustomerId);

                    return SharePointApiResponse<SharePointContact?>.Ok(contact,
                        "Contact retrieved successfully");
                }

                return SharePointApiResponse<SharePointContact?>.Error(
                    "Contact not found", "SP_NOT_FOUND");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("SharePoint item with ID '{itemId}' not found.", id);
                return SharePointApiResponse<SharePointContact?>.Error(
                    "Contact not found or you don't have access", "SP_NOT_FOUND");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return SharePointApiResponse<SharePointContact?>.Error(
                    "Access denied to this SharePoint item", "SP_UNAUTHORIZED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SharePoint item by ID");
                return SharePointApiResponse<SharePointContact?>.Error(
                    "Error retrieving contact", "SP_ERROR");
            }
        }

        public async Task<SharePointApiResponse<List<SharePointContact>>> SearchOpportunitiesAsync(string userToken, string query)
        {
            _logger.LogInformation("Searching for '{query}' in SharePoint list '{listTitle}' via User Context.", query, _listTitle);

            if (string.IsNullOrEmpty(userToken))
            {
                return SharePointApiResponse<List<SharePointContact>>.Error(
                    "User access token is required", "SP_NO_TOKEN");
            }

            try
            {
                var escapedQuery = query.Replace("'", "''");

                var selectFields = new List<string>
                {
                    "Id", "Opportunity_x0020_ID", "Title", /* ... ใส่ Field อื่นๆ ที่ต้องการ ... */
                    "Author/Title", "Editor/Title", "Customer_x0020_Name/Title", "Customer_x0020_Name/Id",
                    "Distributor/Title", "CAE_x0020_in_x002d_charge/Title",
                };

                var expandFields = new List<string>
                {
                    "Author", "Editor", "Customer_x0020_Name", "Distributor", "CAE_x0020_in_x002d_charge",
                };

                var selectQuery = string.Join(",", selectFields);
                var expandQuery = string.Join(",", expandFields);

                // Filter สามารถค้นหาจาก Title และ Customer Name ได้
                var filterQuery = $"substringof('{escapedQuery}', Title) or substringof('{escapedQuery}', Customer_x0020_Name/Title)";

                var endpoint = $"_api/web/lists/getbytitle('{_listTitle}')/items?$filter={filterQuery}&$select={selectQuery}&$expand={expandQuery}";

                // --- ใช้ Logic Two-Step Fetch เหมือนเดิม ---
                var rawOpportunities = await GetItemsFromSharePointAsync(endpoint, userToken, item =>
                    new OpportunityRawData(
                        item.Clone(), // Clone JsonElement ตรงนี้เพื่อใช้ Map ในภายหลัง
                        GetInt(item, "Customer_x0020_Name", "Id") // ดึง Customer Id จาก Opportunity List
                    )
                );

                var customerIds = rawOpportunities
                    .Select(opp => opp.CustomerId)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                var salePersonNameByCustomerId = await GetSalePersonNamesByCustomerIdsAsync(userToken, customerIds);

                var results = rawOpportunities
                    .Select(oppRaw => MapListItemToSharePointContact(oppRaw.Json, salePersonNameByCustomerId))
                    .ToList();

                _logger.LogInformation("Search for '{query}' found {Count} results.", query, results.Count);

                return SharePointApiResponse<List<SharePointContact>>.Ok(results, $"Found {results.Count} matching records");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching SharePoint with User Context for query: {query}", query);
                return SharePointApiResponse<List<SharePointContact>>.Error(
                    "An unexpected error occurred during the search.", "SP_SEARCH_ERROR");
            }
        }

        #region Unchanged Methods (ส่วนนี้ไม่ได้แก้ไข)
        public async Task<SharePointApiResponse<List<SharePointList>>> GetAvailableListsAsync(string userToken)
        {
            _logger.LogInformation("Getting available lists from site using User Context.");

            if (string.IsNullOrEmpty(userToken))
            {
                return SharePointApiResponse<List<SharePointList>>.Error(
                    "User access token is required", "SP_NO_TOKEN");
            }

            var endpoint = "_api/web/lists?$filter=Hidden eq false";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                var jsonResponse = await SendRequestAsync(request, userToken);

                var responseData = JsonSerializer.Deserialize<SharePointRestResponse<SharePointListRestItem>>(jsonResponse, _jsonOptions);
                var lists = responseData?.D?.Results?.Select(MapRestListToSharePointList).ToList() ?? new List<SharePointList>();

                return SharePointApiResponse<List<SharePointList>>.Ok(lists,
                    $"Retrieved {lists.Count} SharePoint lists");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SharePoint lists with User Context");
                return SharePointApiResponse<List<SharePointList>>.Error(
                    "Failed to get lists", "SP_LISTS_ERROR");
            }
        }

        public async Task<UserDiagnosticResponse> DiagnoseUserAsync(string userToken)
        {
            _logger.LogInformation("Diagnosing user SharePoint access...");

            if (string.IsNullOrEmpty(userToken))
            {
                return new UserDiagnosticResponse
                {
                    Success = false,
                    Message = "No user token provided",
                    HasToken = false
                };
            }

            try
            {
                var userEndpoint = "_api/web/currentuser";
                var userRequest = new HttpRequestMessage(HttpMethod.Get, userEndpoint);
                var userResponse = await SendRequestAsync(userRequest, userToken);
                var userInfo = JsonSerializer.Deserialize<JsonElement>(userResponse, _jsonOptions);

                var sitesEndpoint = "_api/web";
                var sitesRequest = new HttpRequestMessage(HttpMethod.Get, sitesEndpoint);
                var sitesResponse = await SendRequestAsync(sitesRequest, userToken);
                var siteInfo = JsonSerializer.Deserialize<JsonElement>(sitesResponse, _jsonOptions);

                return new UserDiagnosticResponse
                {
                    Success = true,
                    HasToken = true,
                    UserInfo = new UserInfo
                    {
                        DisplayName = GetString(userInfo, "Title"),
                        Email = GetString(userInfo, "Email"),
                        Id = GetString(userInfo, "Id")
                    },
                    SharePointAccess = new SharePointAccessInfo
                    {
                        CanAccessSites = true,
                        SiteTitle = GetString(siteInfo, "Title"),
                        SiteUrl = GetString(siteInfo, "Url")
                    },
                    Message = "User context diagnosis completed successfully"
                };
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new UserDiagnosticResponse { Success = false, HasToken = true, Message = "SharePoint access denied. Please check your permissions.", ErrorCode = "UNAUTHORIZED" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User diagnostic failed");
                return new UserDiagnosticResponse { Success = false, HasToken = true, Message = $"Diagnostic failed: {ex.Message}" };
            }
        }

        public async Task<PermissionValidationResponse> ValidateUserPermissionsAsync(string userToken)
        {
            _logger.LogInformation("Validating user permissions...");

            if (string.IsNullOrEmpty(userToken))
            {
                return new PermissionValidationResponse { Success = false, HasAccess = false, Message = "User authentication required" };
            }

            try
            {
                var listEndpoint = $"_api/web/lists/getbytitle('{_listTitle}')";
                var listRequest = new HttpRequestMessage(HttpMethod.Get, listEndpoint);
                var listResponse = await SendRequestAsync(listRequest, userToken);
                var listInfo = JsonSerializer.Deserialize<JsonElement>(listResponse, _jsonOptions);

                var itemsEndpoint = $"_api/web/lists/getbytitle('{_listTitle}')/items?$top=1";
                var itemsRequest = new HttpRequestMessage(HttpMethod.Get, itemsEndpoint);
                var itemsResponse = await SendRequestAsync(itemsRequest, userToken);

                return new PermissionValidationResponse
                {
                    Success = true,
                    HasAccess = true,
                    Permissions = new List<string> { "Read", "Sites.Read.All" },
                    Lists = new List<ListInfo>
                    {
                        new ListInfo { Id = GetString(listInfo, "Id"), Title = GetString(listInfo, "Title"), ItemCount = GetInt(listInfo, "ItemCount") }
                    },
                    Message = "User has valid SharePoint permissions"
                };
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new PermissionValidationResponse { Success = false, HasAccess = false, Message = "Access denied. Please check your SharePoint permissions." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission validation failed");
                return new PermissionValidationResponse { Success = false, HasAccess = false, Message = $"Permission validation failed: {ex.Message}" };
            }
        }

        public async Task<ConnectionTestResponse> TestConnectionAsync(string userToken)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Testing SharePoint connection with User Context...");

            if (string.IsNullOrEmpty(userToken))
            {
                return new ConnectionTestResponse { Success = false, Message = "User authentication required for connection test" };
            }

            try
            {
                var testEndpoint = "_api/web";
                var testRequest = new HttpRequestMessage(HttpMethod.Get, testEndpoint);
                var testResponse = await SendRequestAsync(testRequest, userToken);
                stopwatch.Stop();

                return new ConnectionTestResponse
                {
                    Success = true,
                    Message = "Connection test successful",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    ConnectionDetails = new Dictionary<string, object> { { "hasValidToken", true }, { "canAccessSharePoint", true }, { "responseLength", testResponse.Length } }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                stopwatch.Stop();
                return new ConnectionTestResponse
                {
                    Success = false,
                    Message = $"Connection test failed: {ex.Message}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    ConnectionDetails = new Dictionary<string, object> { { "hasValidToken", true }, { "canAccessSharePoint", false }, { "error", ex.Message } }
                };
            }
        }
        #endregion

        #region Private Helper Methods
        private async Task<string> SendRequestAsync(HttpRequestMessage request, string userToken)
        {
            if (string.IsNullOrEmpty(userToken))
            {
                throw new UnauthorizedAccessException("User access token is required for SharePoint REST API.");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); // ใช้ application/json เป็นหลัก

            _logger.LogDebug("Sending SharePoint request: {Method} {Uri}", request.Method, request.RequestUri);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("SharePoint API error: {StatusCode} - {Content} for URL: {RequestUri}", response.StatusCode, errorContent, request.RequestUri);
                var httpException = new HttpRequestException(errorContent, null, response.StatusCode);
                throw httpException;
            }

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<List<T>> GetItemsFromSharePointAsync<T>(
            string endpoint,
            string userToken,
            Func<JsonElement, T> mapFunction)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            var jsonResponse = await SendRequestAsync(request, userToken);

            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;
                IEnumerable<JsonElement> itemsArray = Enumerable.Empty<JsonElement>();

                if (root.TryGetProperty("d", out var dElement) && dElement.TryGetProperty("results", out var resultsElement))
                {
                    itemsArray = resultsElement.EnumerateArray();
                }
                else if (root.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.Array)
                {
                    itemsArray = valueElement.EnumerateArray();
                }
                else if (root.ValueKind == JsonValueKind.Array) // กรณีที่ root เป็น array โดยตรง (เช่น /_api/web/lists)
                {
                    itemsArray = root.EnumerateArray();
                }
                else
                {
                    _logger.LogWarning("Could not find a valid array ('d.results', 'value', or root array) in SharePoint JSON response for endpoint: {endpoint}. Raw JSON: {json}", endpoint, jsonResponse);
                    return new List<T>();
                }
                var results = itemsArray.Select(mapFunction).ToList();
                return results;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse SharePoint JSON response for endpoint: {endpoint}. Raw JSON: {json}", endpoint, jsonResponse);
                return new List<T>();
            }
        }

        private async Task<JsonElement?> GetSingleItemFromSharePointAsync(string endpoint, string userToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            var jsonResponse = await SendRequestAsync(request, userToken);
            // SharePoint API สำหรับ Single Item จะ Return เป็น JSON Object ตรงๆ หรืออยู่ใน "d" object
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            if (root.TryGetProperty("d", out var dElement) && dElement.ValueKind == JsonValueKind.Object)
            {
                return dElement;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                return root;
            }
            return null; // ไม่มีข้อมูล
        }

        private SharePointList MapRestListToSharePointList(SharePointListRestItem restItem)
        {
            return new SharePointList
            {
                Id = restItem.Id,
                Name = restItem.Title,
                DisplayName = restItem.Title,
                Description = restItem.Description,
                CreatedDateTime = restItem.Created,
                LastModifiedDateTime = restItem.LastItemModifiedDate,
            };
        }


        private int GetInt(JsonElement element, string key, string? subKey = null)
        {
            if (element.TryGetProperty(key, out var prop) && prop.ValueKind != JsonValueKind.Null)
            {
                if (subKey != null)
                {
                    // ลองดึงจาก subKey
                    if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty(subKey, out var subProp) && subProp.ValueKind != JsonValueKind.Null)
                    {
                        if (subProp.TryGetInt32(out var result)) return result;
                    }
                    // หาก subKey เป็น Id และเรา Select มาด้วยชื่อ keyId (เช่น Customer_x0020_NameId)
                    // ตัวอย่าง: Customer_x0020_NameId: 123
                    if (subKey.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        string directIdKey = $"{key}Id";
                        if (element.TryGetProperty(directIdKey, out var directIdProp) && directIdProp.TryGetInt32(out var directIdResult))
                        {
                            return directIdResult;
                        }
                    }
                }
                else // ไม่มี subKey
                {
                    if (prop.TryGetInt32(out var result)) return result;
                }
            }
            return 0;
        }

        private DateTime? GetDateTime(JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out var prop) &&
                prop.ValueKind == JsonValueKind.String) // ไม่จำเป็นต้องเช็ค DateTime.MinValue ใน TryGetDateTime
            {
                if (prop.TryGetDateTimeOffset(out var result)) // ใช้ TryGetDateTimeOffset เพื่อ handle Timezone ได้ดีกว่า
                {
                    return result.DateTime;
                }
            }
            return null;
        }

        private string GetString(JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out var prop) && prop.ValueKind != JsonValueKind.Null)
            {
                return prop.ValueKind switch
                {
                    JsonValueKind.String => prop.GetString() ?? "",
                    JsonValueKind.Number => prop.GetDecimal().ToString(), // แปลง number เป็น string
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => "" // กรณีอื่นๆ
                };
            }
            return "";
        }

        private bool GetBoolean(JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out var prop) && prop.ValueKind != JsonValueKind.Null)
            {
                return prop.ValueKind == JsonValueKind.True;
            }
            return false;
        }
        private decimal GetDecimal(JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out var prop) && prop.ValueKind != JsonValueKind.Null)
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    if (prop.TryGetDecimal(out var numberValue))
                    {
                        return numberValue;
                    }
                }
                else if (prop.ValueKind == JsonValueKind.String)
                {
                    if (decimal.TryParse(prop.GetString(), out var stringValue))
                    {
                        return stringValue;
                    }
                }
            }
            return 0;
        }

        #endregion

        #region Helper Classes for Deserialization
        private class SharePointRestResponse<T> { [JsonPropertyName("d")] public ResultsWrapper<T>? D { get; set; } }
        private class ResultsWrapper<T> { [JsonPropertyName("results")] public IEnumerable<T>? Results { get; set; } }
        // ไม่จำเป็นต้องใช้ SharePointSingleItemResponse<T> ถ้า GetSingleItemFromSharePointAsync ทำการ parse เอง
        // private class SharePointSingleItemResponse<T> { [JsonPropertyName("d")] public T? D { get; set; } } 
        private class SharePointListRestItem
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public DateTime Created { get; set; }
            public DateTime LastItemModifiedDate { get; set; }
        }
        #endregion
    }
}