// IDataService.cs
using DataMatchBackend.Models;

namespace DataMatchBackend.Services;

public interface IDataService
{
    // Customer Data Operations
    Task<List<CustomerDataEntity>> GetAllCustomersAsync();
    Task<PersonDocument> UpsertPersonDocumentAsync(PersonDocument person);
    Task<CustomerDataEntity?> GetCustomerByIdAsync(string id);
    Task<CustomerDataEntity> CreateCustomerAsync(CustomerDataEntity customer);
    Task<CustomerDataEntity> UpdateCustomerAsync(CustomerDataEntity customer);
    Task<bool> DeleteCustomerAsync(string id);
    Task<List<CustomerDataEntity>> SearchCustomersAsync(SearchCriteria criteria);
    Task<List<CustomerDataEntity>> BulkUpdateCustomersAsync(List<CustomerDataEntity> customers);

    // PersonDocument Operations (merged table)
    Task<List<PersonDocument>> GetAllPersonDocumentsAsync();
    Task<PersonDocument?> GetPersonDocumentAsync(string id);
    Task<List<PersonDocument>> GetPersonDocumentsByOpportunityIdAsync(string opportunityId); 
    Task<List<PersonDocument>> SearchPersonDocumentsAsync(SearchCriteria criteria);
    Task<PersonDocument> CreatePersonDocumentAsync(PersonDocument person);
    Task<PersonDocument> UpdatePersonDocumentAsync(PersonDocument person);
    Task<bool> DeletePersonDocumentAsync(string id);
    Task<List<PersonDocument>> BulkUpdatePersonDocumentsAsync(List<PersonDocument> persons);
    Task<List<PersonDocument>> GetRecentlyModifiedRecordsAsync(int days = 30);

    // Match Operations
    Task<List<MatchRecord>> GetMatchHistoryAsync();
    Task<MatchRecord> SaveMatchAsync(MatchedRecord matchedRecord);
    Task<List<MatchRecord>> GetMatchesByStatusAsync(string status);
    Task<MatchRecord> UpdateMatchStatusAsync(string matchId, string status, string approvedBy = "");


    // Statistics
    Task<DataStatistics> GetDataStatisticsAsync();
    Task<List<CustomerDataEntity>> GetRecentCustomersAsync(int days = 30);
    Task<List<MatchRecord>> GetRecentMatchesAsync(int days = 30);

    // Health Check
    Task<bool> IsHealthyAsync();
    Task<Dictionary<string, object>> GetHealthDetailsAsync();
    Task<bool> CreateBackupAsync(string backupName);
    Task<(int deletedCount, int insertedCount)> ReplaceAllPersonDocumentsAsync(List<PersonDocument> newPersons, string partitionKey);

}