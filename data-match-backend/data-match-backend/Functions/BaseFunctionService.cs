using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DataMatchBackend.Helpers;
using DataMatchBackend.Services;
using Sic.Login;

namespace DataMatchBackend.Functions
{
    public abstract class BaseFunctionService
    {
        protected readonly ILogger _logger;
        protected readonly IServiceProvider _serviceProvider;

        protected BaseFunctionService(ILogger logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        // Authentication methods
        protected async Task<(bool IsValid, string? UserToken, string ErrorMessage)> ValidateAuthenticationAsync(HttpRequestData req)
        {
            return await AuthenticationHelper.ValidateConditionalAsync(req, _serviceProvider);
        }

        protected string? ExtractUserToken(HttpRequestData req)
        {
            return AuthenticationHelper.ExtractUserToken(req);
        }

        // Response helpers
        protected async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, System.Net.HttpStatusCode statusCode, string message)
        {
            return await ResponseHelper.CreateErrorResponseAsync<object>(req, statusCode, message);
        }

        protected async Task<HttpResponseData> CreateOkResponse<T>(HttpRequestData req, T data, string message = "Success")
        {
            return await ResponseHelper.CreateOkResponseAsync(req, data, message);
        }

        // Service availability checks
        protected bool IsServiceAvailable<T>() where T : class
        {
            return _serviceProvider.GetService<T>() != null;
        }

        protected async Task<HttpResponseData> CreateServiceUnavailableResponse(HttpRequestData req, string serviceName, string configKey)
        {
            return await ResponseHelper.CreateServiceUnavailableResponseAsync(req, serviceName, configKey);
        }
    }
}