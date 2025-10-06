
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using Sic.Login;
using DataMatchBackend.Services;
using DataMatchBackend.Authentication;
using Microsoft.Extensions.Options;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    
.ConfigureServices((context, services) =>
{
    var configuration = context.Configuration;
    var valuesSection = configuration.GetSection("Values");

    services.Configure<SharePointServiceOptions>(configuration.GetSection("SharePoint"));

    services.AddHttpClient<ISharePointService, SharePointRestService>(client =>
    {
        var baseUrl = configuration["SHAREPOINT_SITE_URL"]; // 
        if (string.IsNullOrEmpty(baseUrl))
        {

            baseUrl = valuesSection["SHAREPOINT_SITE_URL"];
        }

        if (!string.IsNullOrEmpty(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }
    });

    var enableAuth = bool.Parse(configuration["ENABLE_AUTH_SERVICE"] ?? valuesSection["ENABLE_AUTH_SERVICE"] ?? "false");
    var enableSharePoint = bool.Parse(configuration["ENABLE_SHAREPOINT_SERVICE"] ?? valuesSection["ENABLE_SHAREPOINT_SERVICE"] ?? "true");
    var enableMatching = bool.Parse(configuration["ENABLE_MATCHING_SERVICE"] ?? valuesSection["ENABLE_MATCHING_SERVICE"] ?? "true");
    var enableDataService = bool.Parse(configuration["ENABLE_DATA_SERVICE"] ?? valuesSection["ENABLE_DATA_SERVICE"] ?? "true");

    if (enableDataService)
    {
        services.AddScoped<IDataService, TableStorageService>();
    }

    if (enableSharePoint)
    {

    }

    if (enableMatching)
    {
        services.AddScoped<ISimilarityService, SimilarityService>();
    }
    services.AddScoped<IValidationService, ValidationService>();
    if (enableAuth)
    {
        services.AddScoped<AuthenAccess>();
    }


})
    .Build();

try
{
    Console.WriteLine("Starting Azure Functions Host (Test Mode)...");
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Host startup failed: {ex.Message}");
    Environment.Exit(1);
}