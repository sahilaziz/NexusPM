using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Monitoring;

namespace Nexus.API.Controllers.Admin;

/// <summary>
/// Admin Monitoring Controller - Enable/Disable switch
/// </summary>
[ApiController]
[Route("api/admin/monitoring")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class MonitoringAdminController : ControllerBase
{
    private readonly IPrivateMonitoringService _monitoring;
    private readonly ILogger<MonitoringAdminController> _logger;

    public MonitoringAdminController(
        IPrivateMonitoringService monitoring,
        ILogger<MonitoringAdminController> logger)
    {
        _monitoring = monitoring;
        _logger = logger;
    }

    /// <summary>
    /// Monitoring konfiqurasiyasını al
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var config = await _monitoring.GetConfigAsync();
        return Ok(config);
    }

    /// <summary>
    /// Monitoring konfiqurasiyasını yenilə
    /// </summary>
    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] MonitoringConfig config)
    {
        config.ModifiedBy = User.Identity?.Name ?? "unknown";
        await _monitoring.UpdateConfigAsync(config);
        
        _logger.LogInformation("Monitoring config updated by {User}", config.ModifiedBy);
        
        return Ok(new { Message = "Monitoring configuration updated" });
    }

    /// <summary>
    /// Monitoring-i aktiv/deaktiv et (Switch)
    /// </summary>
    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleMonitoring([FromBody] ToggleRequest request)
    {
        var config = await _monitoring.GetConfigAsync();
        config.IsEnabled = request.Enable;
        config.ModifiedBy = User.Identity?.Name ?? "unknown";
        
        await _monitoring.UpdateConfigAsync(config);
        
        var status = request.Enable ? "enabled" : "disabled";
        _logger.LogInformation("Monitoring {Status} by {User}", status, config.ModifiedBy);
        
        return Ok(new { 
            Message = $"Monitoring {status}",
            IsEnabled = request.Enable,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Dashboard məlumatları
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int hours = 1)
    {
        var data = await _monitoring.GetDashboardDataAsync(TimeSpan.FromHours(hours));
        return Ok(data);
    }

    /// <summary>
    /// Real-time status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var config = await _monitoring.GetConfigAsync();
        var data = await _monitoring.GetDashboardDataAsync(TimeSpan.FromMinutes(5));
        
        return Ok(new
        {
            IsEnabled = config.IsEnabled,
            LogRequests = config.LogRequests,
            LogErrors = config.LogErrors,
            TrackPerformance = config.TrackPerformance,
            RetentionDays = config.RetentionDays,
            CurrentMetrics = data
        });
    }
}

public class ToggleRequest
{
    public bool Enable { get; set; }
}
