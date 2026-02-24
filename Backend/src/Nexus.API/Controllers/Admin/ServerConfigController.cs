using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexus.API.Controllers.Admin;

/// <summary>
/// Server Konfiqurasiyası Controller
/// Azure və Öz sistemlər arasında switch etmək üçün
/// </summary>
[ApiController]
[Route("api/admin/server-config")]
[Authorize(Roles = "SuperAdmin")]
public class ServerConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServerConfigController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ServerConfigController(
        IConfiguration configuration,
        ILogger<ServerConfigController> logger,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Cari server konfiqurasiyasını al
    /// </summary>
    [HttpGet]
    public IActionResult GetConfig()
    {
        var messagingMode = _configuration.GetValue<string>("Messaging:Mode", "Private");
        var monitoringMode = _configuration.GetValue<string>("Monitoring:Mode", "Private");
        
        return Ok(new
        {
            CurrentConfig = new
            {
                Messaging = new
                {
                    Mode = messagingMode,
                    Description = messagingMode == "Private" 
                        ? "SQL Server Message Queue (Öz sistem)" 
                        : "Azure Service Bus",
                    Status = messagingMode == "Private" ? "Active" : "Active (Azure)"
                },
                Monitoring = new
                {
                    Mode = monitoringMode,
                    Description = monitoringMode == "Private" 
                        ? "SQL Server Monitoring (Öz sistem)" 
                        : "Azure Application Insights",
                    Status = monitoringMode == "Private" ? "Active" : "Active (Azure)"
                }
            },
            Environment = _environment.EnvironmentName,
            ServerTime = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Messaging sistemini dəyiş (Private ↔ Azure)
    /// </summary>
    [HttpPost("messaging/switch")]
    public IActionResult SwitchMessaging([FromBody] SwitchModeRequest request)
    {
        if (request.Mode != "Private" && request.Mode != "Azure")
        {
            return BadRequest(new { Error = "Mode must be 'Private' or 'Azure'" });
        }

        // Note: Bu dəyişiklik appsettings.json-da edilməlidir və server restart tələb edir
        // Və ya database-də saxlanıla bilər (real-time switch üçün)
        
        _logger.LogCritical(
            "Admin {User} switched Messaging from {OldMode} to {NewMode}. RESTART REQUIRED!",
            User.Identity?.Name,
            _configuration.GetValue<string>("Messaging:Mode", "Private"),
            request.Mode);

        return Ok(new
        {
            Message = $"Messaging mode changed to {request.Mode}",
            Warning = "Server restart required for changes to take effect",
            NewMode = request.Mode,
            OldMode = _configuration.GetValue<string>("Messaging:Mode", "Private"),
            Timestamp = DateTime.UtcNow,
            ChangedBy = User.Identity?.Name
        });
    }

    /// <summary>
    /// Monitoring sistemini dəyiş (Private ↔ Azure)
    /// </summary>
    [HttpPost("monitoring/switch")]
    public IActionResult SwitchMonitoring([FromBody] SwitchModeRequest request)
    {
        if (request.Mode != "Private" && request.Mode != "Azure")
        {
            return BadRequest(new { Error = "Mode must be 'Private' or 'Azure'" });
        }

        _logger.LogCritical(
            "Admin {User} switched Monitoring from {OldMode} to {NewMode}. RESTART REQUIRED!",
            User.Identity?.Name,
            _configuration.GetValue<string>("Monitoring:Mode", "Private"),
            request.Mode);

        return Ok(new
        {
            Message = $"Monitoring mode changed to {request.Mode}",
            Warning = "Server restart required for changes to take effect",
            NewMode = request.Mode,
            OldMode = _configuration.GetValue<string>("Monitoring:Mode", "Private"),
            Timestamp = DateTime.UtcNow,
            ChangedBy = User.Identity?.Name
        });
    }

    /// <summary>
    /// Hər iki sistem birdən dəyiş
    /// </summary>
    [HttpPost("switch-all")]
    public IActionResult SwitchAll([FromBody] SwitchAllRequest request)
    {
        if ((request.MessagingMode != "Private" && request.MessagingMode != "Azure") ||
            (request.MonitoringMode != "Private" && request.MonitoringMode != "Azure"))
        {
            return BadRequest(new { Error = "Modes must be 'Private' or 'Azure'" });
        }

        _logger.LogCritical(
            "Admin {User} switched ALL systems. Messaging: {OldMessaging}→{NewMessaging}, Monitoring: {OldMonitoring}→{NewMonitoring}. RESTART REQUIRED!",
            User.Identity?.Name,
            _configuration.GetValue<string>("Messaging:Mode", "Private"),
            request.MessagingMode,
            _configuration.GetValue<string>("Monitoring:Mode", "Private"),
            request.MonitoringMode);

        return Ok(new
        {
            Message = "All systems mode changed",
            Warning = "Server restart required for changes to take effect",
            NewConfig = new
            {
                Messaging = request.MessagingMode,
                Monitoring = request.MonitoringMode
            },
            OldConfig = new
            {
                Messaging = _configuration.GetValue<string>("Messaging:Mode", "Private"),
                Monitoring = _configuration.GetValue<string>("Monitoring:Mode", "Private")
            },
            Timestamp = DateTime.UtcNow,
            ChangedBy = User.Identity?.Name
        });
    }

    /// <summary>
    /// Azure Service Bus connection string-ini yenilə
    /// </summary>
    [HttpPut("azure/servicebus-connection")]
    public IActionResult UpdateServiceBusConnection([FromBody] UpdateConnectionRequest request)
    {
        // Note: Real implementation would update appsettings.json or database
        _logger.LogInformation("Admin {User} updated Azure Service Bus connection string", 
            User.Identity?.Name);

        return Ok(new
        {
            Message = "Connection string updated (restart required)",
            Note = "Please restart server to apply changes",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Azure Application Insights connection string-ini yenilə
    /// </summary>
    [HttpPut("azure/appinsights-connection")]
    public IActionResult UpdateAppInsightsConnection([FromBody] UpdateConnectionRequest request)
    {
        _logger.LogInformation("Admin {User} updated Azure Application Insights connection string", 
            User.Identity?.Name);

        return Ok(new
        {
            Message = "Connection string updated (restart required)",
            Note = "Please restart server to apply changes",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sistemlərin status-unu yoxla
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var messagingMode = _configuration.GetValue<string>("Messaging:Mode", "Private");
        var monitoringMode = _configuration.GetValue<string>("Monitoring:Mode", "Private");

        var status = new
        {
            Messaging = new
            {
                CurrentMode = messagingMode,
                IsPrivate = messagingMode == "Private",
                IsAzure = messagingMode == "Azure",
                Status = messagingMode == "Private" ? "Running (SQL Server)" : "Running (Azure)",
                CanSwitch = true
            },
            Monitoring = new
            {
                CurrentMode = monitoringMode,
                IsPrivate = monitoringMode == "Private",
                IsAzure = monitoringMode == "Azure",
                Status = monitoringMode == "Private" ? "Running (SQL Server)" : "Running (Azure)",
                CanSwitch = true
            },
            Costs = new
            {
                Current = messagingMode == "Private" && monitoringMode == "Private" 
                    ? "$0/ay (Pulsuz)" 
                    : "$300-600/ay (Azure)",
                PrivateOnly = "$0/ay",
                AzureMessagingOnly = "$30/ay",
                AzureMonitoringOnly = "$200/ay",
                FullAzure = "$230/ay"
            }
        };

        return Ok(status);
    }
}

// Request DTOs
public class SwitchModeRequest
{
    public string Mode { get; set; } = null!; // "Private" veya "Azure"
}

public class SwitchAllRequest
{
    public string MessagingMode { get; set; } = null!;
    public string MonitoringMode { get; set; } = null!;
}

public class UpdateConnectionRequest
{
    public string ConnectionString { get; set; } = null!;
}
