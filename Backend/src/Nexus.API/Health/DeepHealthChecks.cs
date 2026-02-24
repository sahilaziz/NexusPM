using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Data.SqlClient;

namespace Nexus.API.Health;

/// <summary>
/// Enterprise Deep Health Checks
/// Əsas yoxlanışlardan daha dərin diagnosistika
/// </summary>
public static class DeepHealthChecks
{
    public static IServiceCollection AddDeepHealthChecks(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Database
            .AddSqlServer(
                configuration.GetConnectionString("DefaultConnection")!,
                healthQuery: "SELECT TOP 1 1 FROM DocumentNodes",
                name: "sql-primary",
                failureStatus: HealthStatus.Critical)
            
            .AddSqlServer(
                configuration.GetConnectionString("ReadReplicaConnection")!,
                healthQuery: "SELECT TOP 1 1 FROM DocumentNodes",
                name: "sql-replica",
                failureStatus: HealthStatus.Degraded)
            
            // Cache
            .AddCheck<NCacheHealthCheck>("ncache", HealthStatus.Degraded)
            
            // Storage
            .AddCheck<StorageHealthCheck>("storage", HealthStatus.Degraded)
            
            // External Services
            .AddCheck<AzureServiceBusHealthCheck>("service-bus", HealthStatus.Degraded)
            
            // Memory
            .AddCheck<MemoryHealthCheck>("memory", HealthStatus.Degraded);

        return services;
    }
}

/// <summary>
/// NCache health check
/// </summary>
public class NCacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public NCacheHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.SetStringAsync("health-check", DateTime.UtcNow.ToString(), cancellationToken);
            var value = await _cache.GetStringAsync("health-check", cancellationToken);
            
            if (value != null)
                return HealthCheckResult.Healthy("NCache is operational");
            
            return HealthCheckResult.Unhealthy("NCache read failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"NCache error: {ex.Message}");
        }
    }
}

/// <summary>
/// Memory usage health check
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var memoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
        var maxMemoryMB = 2048; // 2GB threshold
        
        if (memoryMB > maxMemoryMB)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"High memory usage: {memoryMB}MB"));
        }
        
        if (memoryMB > maxMemoryMB * 0.8)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Elevated memory usage: {memoryMB}MB"));
        }
        
        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory usage: {memoryMB}MB"));
    }
}

/// <summary>
/// Azure Service Bus health check
/// </summary>
public class AzureServiceBusHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public AzureServiceBusHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration["AzureServiceBus:ConnectionString"];
        
        if (string.IsNullOrEmpty(connectionString))
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Service Bus not configured"));
        }
        
        // Real implementation would check connectivity
        return Task.FromResult(HealthCheckResult.Healthy("Service Bus configured"));
    }
}
