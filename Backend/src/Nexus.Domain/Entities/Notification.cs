namespace Nexus.Domain.Entities;

/// <summary>
/// İstifadəçi bildirişləri (Real-time + Persistent)
/// </summary>
public class Notification
{
    public long NotificationId { get; set; }
    
    /// <summary>
    /// Bildirişi alan istifadəçi (Active Directory username)
    /// </summary>
    public string RecipientUserId { get; set; } = null!;
    
    /// <summary>
    /// Bildirişi göndərən istifadəçi (null = sistem)
    /// </summary>
    public string? SenderUserId { get; set; }
    
    /// <summary>
    /// Bildiriş tipi
    /// </summary>
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// Başlıq (məsələn: "Yeni tapşırıq təyin edildi")
    /// </summary>
    public string Title { get; set; } = null!;
    
    /// <summary>
    /// Ətraflı mesaj
    /// </summary>
    public string Message { get; set; } = null!;
    
    /// <summary>
    /// Əlaqəli entity tipi (Task, Document, Project və s.)
    /// </summary>
    public string? EntityType { get; set; }
    
    /// <summary>
    /// Əlaqəli entity ID-si
    /// </summary>
    public long? EntityId { get; set; }
    
    /// <summary>
    /// Əlavə məlumat (JSON formatında)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Oxunma statusu
    /// </summary>
    public bool IsRead { get; set; } = false;
    
    /// <summary>
    /// Oxunma tarixi
    /// </summary>
    public DateTime? ReadAt { get; set; }
    
    /// <summary>
    /// Real-time göndərildi mi?
    /// </summary>
    public bool IsDelivered { get; set; } = false;
    
    /// <summary>
    /// Çatdırılma tarixi
    /// </summary>
    public DateTime? DeliveredAt { get; set; }
    
    /// <summary>
    /// Yaradılma tarixi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Təşkilat kodu (təcrid üçün)
    /// </summary>
    public string OrganizationCode { get; set; } = null!;
}

public enum NotificationType
{
    /// <summary>
    /// Yeni tapşırıq təyin edildi
    /// </summary>
    TaskAssigned,
    
    /// <summary>
    /// Tapşırıq statusu dəyişdi
    /// </summary>
    TaskStatusChanged,
    
    /// <summary>
    /// Tapşırıqdeadline yaxınlaşır
    /// </summary>
    TaskDeadlineReminder,
    
    /// <summary>
    /// Tapşırığa şərh əlavə edildi
    /// </summary>
    TaskCommentAdded,
    
    /// <summary>
    /// Yeni sənəd yükləndi
    /// </summary>
    DocumentUploaded,
    
    /// <summary>
    /// Sənəd təsdiqləndi/rədd edildi
    /// </summary>
    DocumentApproved,
    
    /// <summary>
    /// Layihə statusu dəyişdi
    /// </summary>
    ProjectStatusChanged,
    
    /// <summary>
    /// Sistem bildirişi
    /// </summary>
    SystemAlert,
    
    /// <summary>
    /// Məktub/qəbul sənədi əlavə edildi
    /// </summary>
    IncomingDocument,
    
    /// <summary>
    /// Sinxronizasiya xətası
    /// </summary>
    SyncError
}
