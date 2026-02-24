namespace Nexus.Domain.Entities;

/// <summary>
/// Offline-first sync queue
/// Cihaz offline olanda dəyişikliklər burada saxlanılır
/// </summary>
public class SyncQueue
{
    public long QueueId { get; set; }
    public string DeviceId { get; set; } = null!;
    public string OrganizationCode { get; set; } = "AZNEFT_IB";
    public SyncOperation Operation { get; set; }
    public string EntityType { get; set; } = null!; // "Task", "Project", "Document"
    public long EntityId { get; set; }
    
    /// <summary>
    /// JSON payload - entity'nin serialize olunmuş forması
    /// </summary>
    public string Payload { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum SyncOperation
{
    Create,
    Update,
    Delete
}

public enum SyncStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
