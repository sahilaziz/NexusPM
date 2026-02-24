using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repositories;

/// <summary>
/// Monitoring Repository Interface
/// </summary>
public interface IMonitoringRepository
{
    // Logs
    Task AddLogAsync(SystemLog log);
    Task<IEnumerable<SystemLog>> GetLogsAsync(
        DateTime? from = null, 
        DateTime? to = null,
        LogLevel? level = null,
        string? category = null,
        string? searchTerm = null,
        int limit = 100);
    Task<int> GetErrorCountAsync(TimeSpan period);
    Task<int> GetRequestCountAsync(TimeSpan period);
    Task<double> GetAverageResponseTimeAsync(TimeSpan period);
    
    // Performance Metrics
    Task AddMetricAsync(PerformanceMetric metric);
    Task<IEnumerable<PerformanceMetric>> GetMetricsAsync(
        string metricName, 
        DateTime? from = null,
        DateTime? to = null);
    
    // Monitoring Config
    Task<MonitoringConfig> GetConfigAsync();
    Task UpdateConfigAsync(MonitoringConfig config);
    
    // Cleanup
    Task CleanupOldLogsAsync(int retentionDays);
    Task CleanupOldMetricsAsync(int retentionDays);
    
    Task SaveChangesAsync();
}
