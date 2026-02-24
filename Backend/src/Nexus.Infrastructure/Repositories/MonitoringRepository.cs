using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;

namespace Nexus.Infrastructure.Repositories;

public class MonitoringRepository : IMonitoringRepository
{
    private readonly AppDbContext _context;

    public MonitoringRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddLogAsync(SystemLog log)
    {
        await _context.SystemLogs.AddAsync(log);
    }

    public async Task<IEnumerable<SystemLog>> GetLogsAsync(
        DateTime? from = null, 
        DateTime? to = null,
        LogLevel? level = null,
        string? category = null,
        string? searchTerm = null,
        int limit = 100)
    {
        var query = _context.SystemLogs.AsQueryable();

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        if (level.HasValue)
            query = query.Where(l => l.Level == level.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(l => l.Category == category);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(l => l.Message.Contains(searchTerm) || 
                                     (l.Exception != null && l.Exception.Contains(searchTerm)));

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetErrorCountAsync(TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        return await _context.SystemLogs
            .CountAsync(l => l.Timestamp >= cutoff && l.Level >= LogLevel.Error);
    }

    public async Task<int> GetRequestCountAsync(TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        return await _context.SystemLogs
            .CountAsync(l => l.Timestamp >= cutoff && l.Category == "Request");
    }

    public async Task<double> GetAverageResponseTimeAsync(TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        var avg = await _context.SystemLogs
            .Where(l => l.Timestamp >= cutoff && l.Category == "Request" && l.DurationMs.HasValue)
            .AverageAsync(l => (double?)l.DurationMs) ?? 0;
        return avg;
    }

    public async Task AddMetricAsync(PerformanceMetric metric)
    {
        await _context.PerformanceMetrics.AddAsync(metric);
    }

    public async Task<IEnumerable<PerformanceMetric>> GetMetricsAsync(
        string metricName, 
        DateTime? from = null,
        DateTime? to = null)
    {
        var query = _context.PerformanceMetrics
            .Where(m => m.MetricName == metricName);

        if (from.HasValue)
            query = query.Where(m => m.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(m => m.Timestamp <= to.Value);

        return await query
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<MonitoringConfig> GetConfigAsync()
    {
        var config = await _context.MonitoringConfigs
            .FirstOrDefaultAsync();

        if (config == null)
        {
            // Default config
            config = new MonitoringConfig
            {
                IsEnabled = true,
                LogRequests = true,
                LogErrors = true,
                TrackPerformance = true,
                MinimumLogLevel = LogLevel.Information,
                RetentionDays = 30
            };
            _context.MonitoringConfigs.Add(config);
            await _context.SaveChangesAsync();
        }

        return config;
    }

    public async Task UpdateConfigAsync(MonitoringConfig config)
    {
        var existing = await _context.MonitoringConfigs
            .FirstOrDefaultAsync(c => c.ConfigId == config.ConfigId);

        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(config);
        }
        else
        {
            _context.MonitoringConfigs.Add(config);
        }

        await _context.SaveChangesAsync();
    }

    public async Task CleanupOldLogsAsync(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        
        var oldLogs = await _context.SystemLogs
            .Where(l => l.Timestamp < cutoff)
            .ToListAsync();

        _context.SystemLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();
    }

    public async Task CleanupOldMetricsAsync(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        
        var oldMetrics = await _context.PerformanceMetrics
            .Where(m => m.Timestamp < cutoff)
            .ToListAsync();

        _context.PerformanceMetrics.RemoveRange(oldMetrics);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
