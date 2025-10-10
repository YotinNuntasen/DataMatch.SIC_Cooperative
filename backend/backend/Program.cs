using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sic.Login;
using DataMatchBackend.Services;
using Microsoft.Extensions.Options;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
        
        if (context.HostingEnvironment.IsDevelopment())
        {

        }
    })
    .ConfigureFunctionsWebApplication() 
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        var valuesSection = configuration.GetSection("Values")
            .GetChildren()
            .ToDictionary(s => s.Key, s => s.Value); 
       
        var rootConfig = configuration.AsEnumerable()
            
            .Where(x => x.Key != "Values" && !x.Key.StartsWith("Values:")) 
            .ToDictionary(x => x.Key, x => x.Value);

        foreach (var item in rootConfig)
        {
            
            if (!valuesSection.ContainsKey(item.Key))
            {
                valuesSection.Add(item.Key, item.Value);
            }
        }
        
       
        string GetSetting(string key) => valuesSection.TryGetValue(key, out var value) ? value : null;
        
       
        Console.WriteLine("=== Configuration Check ===");
        Console.WriteLine($"SHAREPOINT_SITE_URL: '{GetSetting("SHAREPOINT_SITE_URL")}'");
        Console.WriteLine($"GRAPH_TENANT_ID: '{GetSetting("GRAPH_TENANT_ID")}'");
        Console.WriteLine($"GRAPH_CLIENT_ID: '{GetSetting("GRAPH_CLIENT_ID")}'");

        // อ่าน feature flags
        var enableAuth = bool.Parse(GetSetting("ENABLE_AUTH_SERVICE") ?? "false");
        var enableSharePoint = bool.Parse(GetSetting("ENABLE_SHAREPOINT_SERVICE") ?? "true");
        var enableMatching = bool.Parse(GetSetting("ENABLE_MATCHING_SERVICE") ?? "true");
        var enableDataService = bool.Parse(GetSetting("ENABLE_DATA_SERVICE") ?? "true");

        Console.WriteLine("=== Service Loading Configuration ===");
        Console.WriteLine($"Auth Service: {enableAuth}");
        Console.WriteLine($"SharePoint Service: {enableSharePoint}");
        Console.WriteLine($"Matching Service: {enableMatching}");
        Console.WriteLine($"Data Service: {enableDataService}");

        
  
        if (enableDataService)
        {
            try
            {
                services.AddScoped<IDataService, TableStorageService>();
                Console.WriteLine("✅ Data Service loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Data Service failed to load: {ex.Message}");
            }
        }
        
        
        if (enableMatching)
        {
            try
            {
                services.AddScoped<ISimilarityService, SimilarityService>();
                services.AddScoped<IValidationService, ValidationService>();
                Console.WriteLine("✅ Matching Services loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Matching Services failed to load: {ex.Message}");
            }
        }
        
        if (enableDataService || enableMatching)
        {
            services.AddMemoryCache();
            Console.WriteLine("✅ Memory Cache loaded");
        }

        
        if (enableAuth)
        {
            try
            {
                services.AddScoped<AuthenAccess>();
                Console.WriteLine("✅ Authentication Service loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Authentication Service failed to load: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("⊘ Authentication disabled (saves ~30MB memory)");
        }

      
        if (enableSharePoint)
        {
            try
                {
                    services.Configure<SharePointServiceOptions>(options =>
                    {
                        options.SiteUrl = GetSetting("SHAREPOINT_SITE_URL");
                        options.OpportunityListTitle = GetSetting("SHAREPOINT_OPPORTUNITY_LIST_TITLE");
                    });


                    var sharepointSiteUrl = GetSetting("SHAREPOINT_SITE_URL");
                    if (string.IsNullOrEmpty(sharepointSiteUrl))
                    {
                        throw new InvalidOperationException("SharePoint Service configuration missing: SHAREPOINT_SITE_URL is not set.");
                    }

                    if (!Uri.TryCreate(sharepointSiteUrl, UriKind.Absolute, out var baseAddress))
                    {
                        throw new InvalidOperationException($"Invalid SHAREPOINT_SITE_URL: '{sharepointSiteUrl}'. It must be a valid absolute URL.");
                    }
                    
                    
                    services.AddHttpClient(nameof(SharePointRestService), client =>
                    {
                        client.BaseAddress = baseAddress;
                        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    });

                    services.AddScoped<ISharePointService, SharePointRestService>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<SharePointRestService>>();
                        var options = sp.GetRequiredService<IOptions<SharePointServiceOptions>>();

                        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                        var httpClient = httpClientFactory.CreateClient(nameof(SharePointRestService));

                        return new SharePointRestService(httpClient, logger, options);
                    });

                    Console.WriteLine($"✅ SharePoint Service loaded and configured for HttpClient with base address: {baseAddress}");

                }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical Error: SharePoint Service failed to load. {ex.Message}");
              
                throw; 
            }
        }
        else
        {
            Console.WriteLine("⊘ SharePoint Services disabled");
        }
    })
    .Build();

try
{
    Console.WriteLine("Starting Azure Functions Host with conditional services...");
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Host startup failed due to a critical error during service configuration: {ex.Message}");

   
    Environment.Exit(1);
}