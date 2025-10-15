using System.Collections.Generic;
using System.Threading.Tasks;
using DataMatchBackend.Models;

namespace DataMatchBackend.Services
{
    public interface ISharePointService
    {
        Task<SharePointApiResponse<List<SharePointContact>>> GetOpportunityListAsync(string userToken);
        Task<SharePointApiResponse<SharePointContact?>> GetOpportunityByIdAsync(string userToken, string id);
        Task<SharePointApiResponse<List<SharePointList>>> GetAvailableListsAsync(string userToken);
        Task<SharePointApiResponse<List<SharePointContact>>> SearchOpportunitiesAsync(string userToken, string query);
        
        // เพิ่ม methods ใหม่สำหรับ User Context
        Task<UserDiagnosticResponse> DiagnoseUserAsync(string userToken);
        Task<PermissionValidationResponse> ValidateUserPermissionsAsync(string userToken);
        Task<ConnectionTestResponse> TestConnectionAsync(string userToken);
    }
}