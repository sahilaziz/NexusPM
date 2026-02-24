using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Nexus.API.Health;

public static class EnterpriseHealthChecks
{
    public static IServiceCollection AddEnterpriseHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();
        healthChecksBuilder.AddSqlServer(
            configuration.GetConnectionString("DefaultConnection")!,
            healthQuery: "SELECT 1",
            name: "sql-server",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "database", "critical" });
        healthChecksBuilder.AddRedis(
            configuration.GetConnectionString("Redis")!,
            name: "redis",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "cache", "distributed" });
        healthChecksBuilder.AddCheck<StorageHealthCheck>(
            "storage",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "storage", "files" });
        healthChecksBuilder.AddCheck<DiskSpaceHealthCheck>(
            "disk-space",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "storage", "disk" });
        healthChecksBuilder.AddCheck<SignalRHealthCheck>(
            "signalr",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "realtime", "notifications" });
        healthChecksBuilder.AddCheck<ApplicationHealthCheck>(
            "application",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "app", "self" });
        services.AddSingleton<HealthCheckResponseWriter>();
        return services;
    }

    public static IEndpointConventionBuilder MapEnterpriseHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteResponse,
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });
    }
}

public class StorageHealthCheck : IHealthCheck
{
    private readonly ILogger<StorageHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public StorageHealthCheck(
        ILogger<StorageHealthCheck> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var storagePaths = _configuration.GetSection("Storage:LocalDiskPaths").Get<string[]>() ?? new[] { "D:\\NexusStorage" };
        var results = new Dictionary<string, object>();
        var isHealthy = true;

        foreach (var path in storagePaths)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    results[$"path_{path}"] = new { status = "not_found", path };
                    isHealthy = false;
                    continue;
                }

                var drive = new DriveInfo(Path.GetPathRoot(path) ?? path);
                var availableSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                var totalSpaceGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                var percentFree = (availableSpaceGB / totalSpaceGB) * 100;

                var testFile = Path.Combine(path, $".healthcheck_{Guid.NewGuid()}.tmp");
                await File.WriteAllTextAsync(testFile, DateTime.UtcNow.ToString(), cancellationToken);
                File.Delete(testFile);

                results[$"path_{path}"] = new
                {
                    status = "healthy",
                    path,
                    availableSpaceGB = Math.Round(availableSpaceGB, 2),
                    totalSpaceGB = Math.Round(totalSpaceGB, 2),
                    percentFree = Math.Round(percentFree, 2),
                    writeTest = "passed"
                };

                if (percentFree < 10)
                {
                    isHealthy = false;
                }
            }
            catch (Exception ex)
            {
                results[$"path_{path}"] = new { status = "error", path, error = ex.Message };
                isHealthy = false;
                _logger.LogError(ex, "Storage health check failed for path {Path}", path);
            }
        }

        if (isHealthy)
            return HealthCheckResult.Healthy("All storage backends operational", data: results);
        return HealthCheckResult.Degraded("Some storage backends have issues", data: results);
    }
}

public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly ILogger<DiskSpaceHealthCheck> _logger;
    public DiskSpaceHealthCheck(ILogger<DiskSpaceHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
        var results = new Dictionary<string, object>();
        var isHealthy = true;
        var hasLowDisk = false;

        foreach (var drive in drives)
        {
            var availableSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            var totalSpaceGB = drive.TotalSize / (1024.0 * 1024 * 1024);
            var percentFree = (availableSpaceGB / totalSpaceGB) * 100;

            results[drive.Name] = new
            {
                availableSpaceGB = Math.Round(availableSpaceGB, 2),
                totalSpaceGB = Math.Round(totalSpaceGB, 2),
                percentFree = Math.Round(percentFree, 2),
                driveFormat = drive.DriveFormat
            };

            if (percentFree < 5)
            {
                isHealthy = false;
                _logger.LogError("Critical disk space on {Drive}: {PercentFree}% free", drive.Name, percentFree);
            }
            else if (percentFree < 10)
            {
                hasLowDisk = true;
                _logger.LogWarning("Low disk space on {Drive}: {PercentFree}% free", drive.Name, percentFree);
            }
        }

        if (!isHealthy)
            return Task.FromResult(HealthCheckResult.Unhealthy("Critical disk space on one or more drives", data: results));
        if (hasLowDisk)
            return Task.FromResult(HealthCheckResult.Degraded("Low disk space on one or more drives", data: results));
        return Task.FromResult(HealthCheckResult.Healthy("Disk space OK", data: results));
    }
}

public class SignalRHealthCheck : IHealthCheck
{
    private readonly ILogger<SignalRHealthCheck> _logger;
    private static long _connectionCount;
    private static long _peakConnectionCount;
    public SignalRHealthCheck(ILogger<SignalRHealthCheck> logger)
    {
        _logger = logger;
    }
    public static void IncrementConnections()
    {
        var count = Interlocked.Increment(ref _connectionCount);
        UpdatePeak(count);
    }
    public static void DecrementConnections()
    {
        Interlocked.Decrement(ref _connectionCount);
    }
    private static void UpdatePeak(long count)
    {
        long currentPeak;
        do
        {
            currentPeak = _peakConnectionCount;
            if (count <= currentPeak) return;
        } while (Interlocked.CompareExchange(ref _peakConnectionCount, count, currentPeak) != currentPeak);
    }
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var currentCount = Interlocked.Read(ref _connectionCount);
        var peakCount = Interlocked.Read(ref _peakConnectionCount);
        var data = new Dictionary<string, object>
        {
            ["currentConnections"] = currentCount,
            ["peakConnections"] = peakCount,
            ["status"] = currentCount > 4500 ? "high_load" : "normal"
        };
        if (currentCount > 4500)
            return Task.FromResult(HealthCheckResult.Degraded($"High SignalR connection count: {currentCount}", data: data));
        return Task.FromResult(HealthCheckResult.Healthy($"SignalR connections: {currentCount} current, {peakCount} peak", data: data));
    }
}

public class ApplicationHealthCheck : IHealthCheck
{
    private readonly ILogger<ApplicationHealthCheck> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;
    public ApplicationHealthCheck(ILogger<ApplicationHealthCheck> logger)
    {
        _logger = logger;
    }
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var uptime = DateTime.UtcNow - _startTime;
        var memoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
        var threadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
        var data = new Dictionary<string, object>
        {
            ["uptime"] = uptime.ToString(),
            ["memoryMB"] = memoryMB,
            ["threadCount"] = threadCount,
            ["gcGeneration"] = GC.MaxGeneration,
            ["status"] = "healthy"
        };
        if (memoryMB > 2048)
        {
            _logger.LogWarning("High memory usage detected: {MemoryMB} MB", memoryMB);
            return Task.FromResult(HealthCheckResult.Degraded($"High memory usage: {memoryMB} MB", data: data));
        }
        return Task.FromResult(HealthCheckResult.Healthy("Application running normally", data: data));
    }
}

public static class HealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    description = e.Value.Description,
                    data = e.Value.Data,
                    exception = e.Value.Exception?.Message
                }),
            timestamp = DateTime.UtcNow
        };
        return context.Response.WriteAsJsonAsync(response);
    }
}
