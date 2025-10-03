// Program.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
// using Sic.Login;
// using DataMatchBackend.Services;
// using DataMatchBackend.Authentication;
// using Microsoft.Extensions.Options;

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

        Console.WriteLine("=== Configuration Check (Test Mode) ===");
        Console.WriteLine($"AzureWebJobsStorage configured: {!string.IsNullOrEmpty(valuesSection["AzureWebJobsStorage"])}");
        
        // ================== START: COMMENT OUT FOR TESTING ==================
        
        // var enableAuth = bool.Parse(valuesSection["ENABLE_AUTH_SERVICE"] ?? "false");
        // var enableSharePoint = bool.Parse(valuesSection["ENABLE_SHAREPOINT_SERVICE"] ?? "true");
        // var enableMatching = bool.Parse(valuesSection["ENABLE_MATCHING_SERVICE"] ?? "true");
        // var enableDataService = bool.Parse(valuesSection["ENABLE_DATA_SERVICE"] ?? "true");

        // if (enableDataService)
        // {
        //     services.AddScoped<IDataService, TableStorageService>();
        // }
        // ... (Comment out การ Add Service อื่นๆ ทั้งหมด) ...
        
        // =================== END: COMMENT OUT FOR TESTING ===================
        
        // เหลือไว้แค่ Health check function อาจจะพอ
        // หรือถ้าอยากให้ชัวร์ ก็ comment out ให้หมดก่อน
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