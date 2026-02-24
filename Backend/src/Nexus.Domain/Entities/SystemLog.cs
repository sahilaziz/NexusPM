namespace Nexus.Domain.Entities;

/// <summary>
/// Sistem log-ları - Monitoring üçün
/// Tam şəxsi, xarici dependency yoxdur
/// </summary>
public class SystemLog
{
    public long LogId { get; set; }
    
    /// <summary>
    /// Log səviyyəsi (Info, Warning, Error, Critical)
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Information;
    
    /// <summary>
    /// Log kateqoriyası (Request, Database, Auth, System)
    /// </summary>
    public string Category { get; set; } = null!;
    
    /// <summary>
    /// Mesa
    /// </summary>
    public string Message { get; set; } = null!;
    
    /// <summary>
    /// Ətraflı məlumat (JSON)
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Exception (əgər varsa)
    /// </summary>
    public string? Exception { get; set; }
    
    /// <summary>
    /// Endpoint/Route
    /// </summary>
    public string? Endpoint { get; set; }
    
    /// <summary>
    /// HTTP Method
    /// </summary>
    public string? HttpMethod { get; set; }
    
    /// <summary>
    /// Status Code
    /// </summary>
    public int? StatusCode { get; set; }
    
    /// <summary>
    /// İcra müddəti (millisaniyə)
    /// </summary>
    public long? DurationMs { get; set; }
    
    /// <summary>
    /// IP ünvanı
    /// </summary>
    public string? ClientIp { get; set; }
    
    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// User Agent
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Təşkilat kodu
    /// </summary>
    public string OrganizationCode { get; set; } = "default";
    
    /// <summary>
    /// Correlation ID (request chain üçün)
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Server/machine adı
    /// </summary>
    public string? MachineName { get; set; }
}

/// <summary>
/// Performance Metrics - Real-time monitoring
/// </summary>
public class PerformanceMetric
{
    public long MetricId { get; set; }
    
    /// <summary>
    /// Metric adı (CpuUsage, MemoryUsage, RequestCount, vb.)
    /// </summary>
    public string MetricName { get; set; } = null!;
    
    /// <summary>
    /// Dəyər
    /// </summary>
    public double Value { get; set; }
    
    /// <summary>
    /// Vahid (%, MB, ms, count)
    /// </summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Tags (JSON)
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Server adı
    /// </summary>
    public string? MachineName { get; set; }
}

/// <summary>
/// Monitoring konfiqurasiyası - Admin paneldən idarə olunur
/// </summary>
public class MonitoringConfig
{
    public long ConfigId { get; set; } = 1;
    
    /// <summary>
    /// Monitoring aktivdir?
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Request logging aktivdir?
    /// </summary>
    public bool LogRequests { get; set; } = true;
    
    /// <summary>
    /// Error logging aktivdir?
    /// </summary>
    public bool LogErrors { get; set; } = true;
    
    /// <summary>
    /// Performance tracking aktivdir?
    /// </summary>
    public bool TrackPerformance { get; set; } = true;
    
    /// <summary>
    /// Database query logging aktivdir?
    /// </summary>
    public bool LogDatabaseQueries { get; set; } = false;
    
    /// <summary>
    /// Minimum log səviyyəsi
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
    
    /// <summary>
    /// Nə qədər saxlanılsın (gün)
    /// </summary>
    public int RetentionDays { get; set; } = 30;
    
    /// <summary>
    /// Slow request threshold (ms)
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 1000;
    
    /// <summary>
    /// Alert email (əgər varsa)
    /// </summary>
    public string? AlertEmail { get; set; }
    
    /// <summary>
    /// CPU threshold for alert (%)
    /// </summary>
    public int CpuAlertThreshold { get; set; } = 80;
    
    /// <summary>
    /// Memory threshold for alert (%)
    /// </summary>
    public int MemoryAlertThreshold { get; set; } = 85;
    
    /// <summary>
    /// Error rate threshold for alert (%)
    /// </summary>
    public int ErrorRateAlertThreshold { get; set; } = 5;
    
    /// <summary>
    /// Son yeniləmə
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
    
    /// <summary>
    /// Kim tərəfindən
    /// </summary>
    public string? ModifiedBy { get; set; }
}

public enum LogLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
