using Microsoft.Azure.Functions.Worker.Http;
using DataMatchBackend.Models;
using System.Net;

namespace DataMatchBackend.Helpers
{
    public static class ResponseHelper
    {
        public static async Task<HttpResponseData> CreateErrorResponseAsync<T>(
            HttpRequestData req, 
            HttpStatusCode statusCode, 
            string message, 
            List<string>? errors = null)
        {
            var response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(new ApiResponse<T>
            {
                Code = (int)statusCode,
                Message = message,
                Success = false,
                Errors = errors ?? new List<string> { message }
            });
            return response;
        }

        public static async Task<HttpResponseData> CreateOkResponseAsync<T>(
            HttpRequestData req, 
            T data, 
            string message = "Success")
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<T>.Ok(data, message));
            return response;
        }

        public static async Task<HttpResponseData> CreateServiceUnavailableResponseAsync(
            HttpRequestData req, 
            string serviceName, 
            string configKey)
        {
            var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await response.WriteAsJsonAsync(new ApiResponse<object>
            {
                Code = 503,
                Message = $"{serviceName} service is not available",
                Success = false,
                Errors = new List<string> { 
                    $"Set {configKey}=true to enable {serviceName}",
                    $"Ensure {serviceName} service is registered in DI container"
                }
            });
            return response;
        }
    }
}