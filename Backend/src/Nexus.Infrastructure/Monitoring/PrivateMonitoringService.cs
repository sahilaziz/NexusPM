using System.Diagnostics;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Monitoring;

/// <summary>
/// Tam şəxsi Monitoring Sistemi
/// Admin paneldən enable/disable etmək olar
/// </summary>
public interface IPrivateMonitoringService
{
    bool IsEnabled { get; }
    Task LogRequestAsync(HttpContext context, long durationMs, int statusCode);
    Task LogErrorAsync(Exception ex, HttpContext? context = null);
    Task LogInfoAsync(string message, string category, object? details = null);
    Task<MonitoringConfig> GetConfigAsync();
    Task UpdateConfigAsync(MonitoringConfig config);
    Task<MonitoringDashboardDto> GetDashboardDataAsync(TimeSpan period);
}

public class PrivateMonitoringService : IPrivateMonitoringService
{
    private readonly IMonitoringRepository _repository;
    private readonly ILogger<PrivateMonitoringService> _logger;

    public PrivateMonitoringService(
        IMonitoringRepository repository,
        ILogger<PrivateMonitoringService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public bool IsEnabled => true; // Config-dən oxunacaq

    public async Task LogRequestAsync(HttpContext context, long durationMs, int statusCode)
    {
        var log = new SystemLog
        {
            Level = durationMs > 1000 ? LogLevel.Warning : LogLevel.Information,
            Category = "Request",
            Message = $"{context.Request.Method} {context.Request.Path} completed",
            Endpoint = context.Request.Path,
            HttpMethod = context.Request.Method,
            StatusCode = statusCode,
            DurationMs = durationMs,
            ClientIp = context.Connection.RemoteIpAddress?.ToString(),
            UserId = context.User.Identity?.Name,
            MachineName = Environment.MachineName
        };

        await _repository.AddLogAsync(log);
        await _repository.SaveChangesAsync();
    }

    public async Task LogErrorAsync(Exception ex, HttpContext? context = null)
    {
        var log = new SystemLog
        {
            Level = LogLevel.Error,
            Category = "Error",
            Message = ex.Message,
            Exception = ex.ToString(),
            Endpoint = context?.Request.Path,
            MachineName = Environment.MachineName
        };

        await _repository.AddLogAsync(log);
        await _repository.SaveChangesAsync();
    }

    public async Task LogInfoAsync(string message, string category, object? details = null)
    {
        var log = new SystemLog
        {
            Level = LogLevel.Information,
            Category = category,
            Message = message,
            MachineName = Environment.MachineName
        };

        await _repository.AddLogAsync(log);
        await _repository.SaveChangesAsync();
    }

    public async Task<MonitoringConfig> GetConfigAsync()
    {
        return await _repository.GetConfigAsync();
    }

    public async Task UpdateConfigAsync(MonitoringConfig config)
    {
        config.ModifiedAt = DateTime.UtcNow;
        await _repository.UpdateConfigAsync(config);
    }

    public async Task<MonitoringDashboardDto> GetDashboardDataAsync(TimeSpan period)
    {
        var logs = await _repository.GetLogsAsync(
            DateTime.UtcNow - period, 
            DateTime.UtcNow, 
            limit: 1000);
        
        return new MonitoringDashboardDto
        {
            TotalRequests = logs.Count(l => l.Category == "Request"),
            ErrorCount = logs.Count(l => l.Level >= LogLevel.Error),
            AverageResponseTime = logs.Where(l => l.DurationMs.HasValue).Average(l => l.DurationMs) ?? 0
        };
    }
}

public class MonitoringDashboardDto
{
    public int TotalRequests { get; set; }
    public int ErrorCount { get; set; }
    public double AverageResponseTime { get; set; }
}
