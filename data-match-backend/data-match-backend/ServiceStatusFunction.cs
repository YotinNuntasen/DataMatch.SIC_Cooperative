using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; 
using DataMatchBackend.Models;
using DataMatchBackend.Services;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sic.Login;

namespace DataMatchBackend.Functions;

public class ServiceStatusFunction
{
    private readonly ILogger<ServiceStatusFunction> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ServiceStatusFunction(ILogger<ServiceStatusFunction> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Get comprehensive service status - สำหรับตรวจสอบ services ทั้งหมด
    /// </summary>
    [Function("GetServiceStatus")]
    public async Task<HttpResponseData> GetServiceStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Getting comprehensive service status");

            // ตรวจสอบ services ทั้งหมดใน DI container
            var services = new Dictionary<string, bool>();

            // ตรวจสอบแต่ละ service ว่ามีใน DI container ไหม
            services["DataService"] = req.FunctionContext.InstanceServices.GetService<IDataService>() != null;
            services["AuthService"] = req.FunctionContext.InstanceServices.GetService<AuthenAccess>() != null;
            services["SharePointService"] = req.FunctionContext.InstanceServices.GetService<ISharePointService>() != null;
            services["MatchingService"] = req.FunctionContext.InstanceServices.GetService<ISimilarityService>() != null;
            services["ValidationService"] = req.FunctionContext.InstanceServices.GetService<IValidationService>() != null;

            // อ่าน feature flags จาก environment variables
            var featureFlags = new Dictionary<string, string>
            {
                { "enableAuth", Environment.GetEnvironmentVariable("ENABLE_AUTH_SERVICE") ?? "false" },
                { "enableSharePoint", Environment.GetEnvironmentVariable("ENABLE_SHAREPOINT_SERVICE") ?? "false" },
                { "enableMatching", Environment.GetEnvironmentVariable("ENABLE_MATCHING_SERVICE") ?? "false" },
                { "enableDataService", Environment.GetEnvironmentVariable("ENABLE_DATA_SERVICE") ?? "true" },
                { "enableAnalytics", Environment.GetEnvironmentVariable("ENABLE_ANALYTICS") ?? "false" },
                { "enableApplicationInsights", Environment.GetEnvironmentVariable("ENABLE_APPLICATION_INSIGHTS") ?? "false" }
            };

            // ตรวจสอบความสอดคล้องระหว่าง feature flags และ service availability
            var inconsistencies = new List<string>();

            if (bool.Parse(featureFlags["enableAuth"]) && !services["AuthService"])
                inconsistencies.Add("Auth service enabled but not available");

            if (bool.Parse(featureFlags["enableSharePoint"]) && !services["SharePointService"])
                inconsistencies.Add("SharePoint service enabled but not available");

            if (bool.Parse(featureFlags["enableMatching"]) && !services["MatchingService"])
                inconsistencies.Add("Matching service enabled but not available");

            // นับ services ที่พร้อมใช้งาน
            var availableServicesCount = services.Count(s => s.Value);
            var totalServicesCount = services.Count;

            // ดึงข้อมูล memory usage
            var memoryInfo = GetMemoryInformation();

            // ตรวจสอบ configuration ที่สำคัญ
            var criticalConfig = CheckCriticalConfiguration();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                timestamp = DateTime.UtcNow,                        
                services = services,                                // สถานะ services ทั้งหมด
                featureFlags = featureFlags,                       // feature flags ทั้งหมด
                inconsistencies = inconsistencies,                 // ความไม่สอดคล้อง
                summary = new
                {
                    availableServices = availableServicesCount,    // จำนวน services ที่พร้อมใช้งาน
                    totalServices = totalServicesCount,            // จำนวน services ทั้งหมด
                    healthyPercentage = (double)availableServicesCount / totalServicesCount * 100, 
                    hasInconsistencies = inconsistencies.Any()     
                },
                memory = memoryInfo,                               
                configuration = criticalConfig,                
                recommendations = GetRecommendations(services, featureFlags, inconsistencies) 
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service status");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Failed to get service status",
                message = ex.Message,
                timestamp = DateTime.UtcNow
            });
            return errorResponse;
        }
    }

    /// <summary>
    /// Get quick health status - endpoint ง่ายๆ สำหรับ monitoring
    /// </summary>
    [Function("QuickHealth")]
    public async Task<HttpResponseData> QuickHealth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "quick-health")] HttpRequestData req)
    {
        try
        {
            // ตรวจสอบ memory usage อย่างง่าย
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / 1024 / 1024;
            var isHealthy = memoryMB < 500; // ถือว่า healthy ถ้าใช้ memory น้อยกว่า 500MB

            // ตรวจสอบ core service
            var dataServiceAvailable = req.FunctionContext.InstanceServices.GetService<IDataService>() != null;

            var overallHealthy = isHealthy && dataServiceAvailable;

            var statusCode = overallHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
            var response = req.CreateResponse(statusCode);

            await response.WriteAsJsonAsync(new
            {
                status = overallHealthy ? "OK" : "Warning",        // สถานะโดยรวม
                timestamp = DateTime.UtcNow,                       // เวลาตรวจสอบ
                memoryMB = memoryMB,                              // การใช้ memory
                dataServiceAvailable = dataServiceAvailable,      // core service พร้อมไหม
                isHealthy = overallHealthy                        // สุขภาพโดยรวม
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in quick health check");

            var errorResponse = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await errorResponse.WriteAsJsonAsync(new
            {
                status = "Error",
                message = ex.Message,
                timestamp = DateTime.UtcNow,
                isHealthy = false
            });
            return errorResponse;
        }
    }

    // Helper Methods

    /// <summary>
    /// ดึงข้อมูล memory และ performance
    /// </summary>
    private object GetMemoryInformation()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();

            return new
            {
                workingSetMB = process.WorkingSet64 / 1024 / 1024,                    // หน่วยความจำที่ใช้
                privateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024,          // private memory
                virtualMemoryMB = process.VirtualMemorySize64 / 1024 / 1024,          // virtual memory
                gcTotalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,            // managed memory
                threadCount = process.Threads.Count,                                  // จำนวน threads
                handleCount = process.HandleCount,                                    // จำนวน handles
                startTime = process.StartTime,                                        // เวลาเริ่มต้น
                totalProcessorTime = process.TotalProcessorTime.TotalSeconds,         // เวลา CPU รวม
                isMemoryHealthy = (process.WorkingSet64 / 1024 / 1024) < 500          // สุขภาพ memory
            };
        }
        catch (Exception ex)
        {
            return new
            {
                error = ex.Message,
                isMemoryHealthy = false
            };
        }
    }

    /// <summary>
    /// ตรวจสอบ configuration ที่สำคัญ
    /// </summary>
    // ในไฟล์ ServiceStatusFunction.cs
    private object CheckCriticalConfiguration()
    {
        var criticalSettings = new Dictionary<string, object>();

        // ... (ส่วนของ Database, SharePoint, Auth เหมือนเดิม) ...
        criticalSettings["azureStorageConfigured"] = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        criticalSettings["tableNameConfigured"] = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TableName"));
        criticalSettings["sharePointConfigured"] = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SHAREPOINT_MAIN_SITE_ID"));
        criticalSettings["graphClientConfigured"] = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID"));
        criticalSettings["jwtSecretConfigured"] = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_SECRET"));

        // ==============================================================================
        // === START: ส่วนที่แก้ไขเพื่อจัดการ Warning CS8601 ===
        // ==============================================================================

        // ใช้ ?? เพื่อกำหนดค่าดีฟอลต์เป็น "Not Set" หากค่าที่ได้เป็น null
        criticalSettings["runtime"] = Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME") ?? "Not Set";
        criticalSettings["environment"] = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development"; 

        // นับการตั้งค่าที่สมบูรณ์
        var configuredCount = criticalSettings.Count(c =>
        {
            if (c.Value is bool b) return b;
            if (c.Value is string s) return !string.IsNullOrEmpty(s) && s != "Not Set";
            return c.Value != null;
        });
        var totalCount = criticalSettings.Count;

        return new
        {
            settings = criticalSettings,
            configuredCount,
            totalCount,
            completenessPercentage = totalCount > 0 ? (double)configuredCount / totalCount * 100 : 0,
            isFullyConfigured = configuredCount == totalCount
        };
    }

    /// <summary>
    /// สร้างคำแนะนำตามสถานะปัจจุบัน
    /// </summary>
    private List<string> GetRecommendations(
        Dictionary<string, bool> services,
        Dictionary<string, string> featureFlags,
        List<string> inconsistencies)
    {
        var recommendations = new List<string>();

        // แนะนำเกี่ยวกับ memory
        var memoryInfo = GetMemoryInformation();
        if (memoryInfo is IDictionary<string, object> memDict &&
            memDict.TryGetValue("workingSetMB", out var memValue) &&
            memValue is long memMB && memMB > 400)
        {
            recommendations.Add($"High memory usage detected ({memMB}MB). Consider disabling non-essential services.");
        }

        // แนะนำเกี่ยวกับ inconsistencies
        if (inconsistencies.Any())
        {
            recommendations.Add("Fix service configuration inconsistencies before enabling features.");
        }

        // แนะนำการปรับแต่งสำหรับ development
        if (!services["AuthService"] && bool.Parse(featureFlags["enableAuth"]))
        {
            recommendations.Add("Set ENABLE_AUTH_SERVICE=false for development without authentication.");
        }

        // แนะนำสำหรับ minimal setup
        if (services.Count(s => s.Value) < 2)
        {
            recommendations.Add("Enable at least DataService for basic functionality.");
        }

        // แนะนำการใช้ memory อย่างมีประสิทธิภาพ
        if (services["SharePointService"] && services["MatchingService"])
        {
            recommendations.Add("SharePoint and Matching services use significant memory. Consider enabling one at a time during development.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("System configuration looks good!");
        }

        return recommendations;
    }
}

/// <summary>
/// Service Status Response Model
/// </summary>
public class ServiceStatusResponse
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("services")]
    public Dictionary<string, ServiceInfo> Services { get; set; } = new();

    [JsonPropertyName("featureFlags")]
    public Dictionary<string, bool> FeatureFlags { get; set; } = new();

    [JsonPropertyName("summary")]
    public ServiceSummary Summary { get; set; } = new();

    [JsonPropertyName("memory")]
    public MemoryInfo Memory { get; set; } = new();

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; set; } = new();
}

public class ServiceInfo
{
    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("memoryImpact")]
    public string MemoryImpact { get; set; } = ""; // Low, Medium, High

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();
}

public class ServiceSummary
{
    [JsonPropertyName("availableServices")]
    public int AvailableServices { get; set; }

    [JsonPropertyName("totalServices")]
    public int TotalServices { get; set; }

    [JsonPropertyName("healthyPercentage")]
    public double HealthyPercentage { get; set; }

    [JsonPropertyName("hasInconsistencies")]
    public bool HasInconsistencies { get; set; }
}

public class MemoryInfo
{
    [JsonPropertyName("workingSetMB")]
    public long WorkingSetMB { get; set; }

    [JsonPropertyName("isHealthy")]
    public bool IsHealthy { get; set; }

    [JsonPropertyName("threshold")]
    public int Threshold { get; set; } = 500;

    [JsonPropertyName("gcTotalMemoryMB")]
    public long GcTotalMemoryMB { get; set; }
}