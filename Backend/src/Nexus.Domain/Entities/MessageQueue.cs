namespace Nexus.Domain.Entities;

/// <summary>
/// Message Queue Entity - Database-based message queue
/// Tam şəxsi, xarici dependency yoxdur
/// </summary>
public class MessageQueue
{
    public long MessageId { get; set; }
    
    /// <summary>
    /// Queue adı (məsələn: "notifications", "emails", "indexing")
    /// </summary>
    public string QueueName { get; set; } = null!;
    
    /// <summary>
    /// Message tipi (Event class adı)
    /// </summary>
    public string MessageType { get; set; } = null!;
    
    /// <summary>
    /// JSON formatında message payload
    /// </summary>
    public string Payload { get; set; } = null!;
    
    /// <summary>
    /// Message status
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    
    /// <summary>
    /// Neçə dəfə cəhd edilib
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Maksimum cəhd sayı
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Növbəti cəhd vaxtı (delay üçün)
    /// </summary>
    public DateTime? ScheduledFor { get; set; }
    
    /// <summary>
    /// Error mesajı (əgər uğursuz olubsa)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Priority (0 = highest)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Unique correlation ID
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Təşkilat kodu (multi-tenant)
    /// </summary>
    public string OrganizationCode { get; set; } = "default";
    
    /// <summary>
    /// Kim tərəfindən yaradılıb
    /// </summary>
    public string CreatedBy { get; set; } = "system";
    
    /// <summary>
    /// Yaradılma vaxtı
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// İşlənmə vaxtı
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Timeout vaxtı (əgər işlənməzsə)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

public enum MessageStatus
{
    /// <summary>
    /// Gözləyir
    /// </summary>
    Pending,
    
    /// <summary>
    /// İşlənir
    /// </summary>
    Processing,
    
    /// <summary>
    /// Uğurla tamamlandı
    /// </summary>
    Completed,
    
    /// <summary>
    /// Uğursuz oldu, retry gözləyir
    /// </summary>
    Failed,
    
    /// <summary>
    /// Bütün retry-lər bitdi, uğursuz
    /// </summary>
    DeadLetter,
    
    /// <summary>
    /// Ləğv edildi
    /// </summary>
    Cancelled
}

/// <summary>
/// Dead Letter Queue - Uğursuz message-lər üçün
/// </summary>
public class DeadLetterMessage
{
    public long DeadLetterId { get; set; }
    public long OriginalMessageId { get; set; }
    public string QueueName { get; set; } = null!;
    public string MessageType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    public string OrganizationCode { get; set; } = "default";
}
