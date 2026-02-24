namespace Nexus.Domain.Entities;

/// <summary>
/// Email şablonları
/// </summary>
public class EmailTemplate
{
    public long TemplateId { get; set; }
    
    /// <summary>
    /// Şablon kodu (unique identifier)
    /// </summary>
    public string TemplateCode { get; set; } = null!;
    
    /// <summary>
    /// Şablon adı
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Email mövzu (subject) şablonu
    /// </summary>
    public string SubjectTemplate { get; set; } = null!;
    
    /// <summary>
    /// Email məzmun (body) şablonu (HTML)
    /// </summary>
    public string BodyTemplate { get; set; } = null!;
    
    /// <summary>
    /// Plain text versiyası (fallback)
    /// </summary>
    public string? PlainTextTemplate { get; set; }
    
    /// <summary>
    /// Şablon tipi
    /// </summary>
    public EmailTemplateType Type { get; set; } = EmailTemplateType.System;
    
    /// <summary>
    /// Aktivdir?
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Varsayılan şablondur?
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// Dil kodu (az, en, ru)
    /// </summary>
    public string LanguageCode { get; set; } = "az";
    
    /// <summary>
    /// Yaradılma tarixi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Son yenilənmə tarixi
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}

public enum EmailTemplateType
{
    System,      // Sistem şablonları
    UserDefined, // İstifadəçi yaratmış şablonlar
    Notification // Bildiriş şablonları
}

/// <summary>
/// Göndərilmiş emaillərin qeydləri
/// </summary>
public class EmailLog
{
    public long EmailLogId { get; set; }
    
    /// <summary>
    /// Kimə göndərilib
    /// </summary>
    public string ToEmail { get; set; } = null!;
    
    /// <summary>
    /// Kimdən göndərilib
    /// </summary>
    public string FromEmail { get; set; } = null!;
    
    /// <summary>
    /// Mövzu
    /// </summary>
    public string Subject { get; set; } = null!;
    
    /// <summary>
    /// İstifadə olunan şablon
    /// </summary>
    public string? TemplateCode { get; set; }
    
    /// <summary>
    /// Email tipi
    /// </summary>
    public EmailType Type { get; set; }
    
    /// <summary>
    /// Göndərilmə statusu
    /// </summary>
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    
    /// <summary>
    /// Xəta mesajı (əgər uğursuz olubsa)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Yenidən cəhd sayı
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Son yenidən cəhd tarixi
    /// </summary>
    public DateTime? LastRetryAt { get; set; }
    
    /// <summary>
    /// Göndərilmə tarixi
    /// </summary>
    public DateTime? SentAt { get; set; }
    
    /// <summary>
    /// Açılma tarixi (tracking)
    /// </summary>
    public DateTime? OpenedAt { get; set; }
    
    /// <summary>
    /// Klikləmə tarixi (tracking)
    /// </summary>
    public DateTime? ClickedAt { get; set; }
    
    /// <summary>
    /// İzləmə ID-si (tracking pixel üçün)
    /// </summary>
    public string? TrackingId { get; set; }
    
    /// <summary>
    /// Əlaqəli entity (Task, Project, vb.)
    /// </summary>
    public string? RelatedEntityType { get; set; }
    public long? RelatedEntityId { get; set; }
    
    /// <summary>
    /// Yaradılma tarixi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum EmailType
{
    Welcome,           // Xoş gəlmisiniz
    PasswordReset,     // Şifrə sıfırlama
    EmailConfirmation, // Email təsdiqi
    TaskAssigned,      // Tapşırıq təyinatı
    TaskCompleted,     // Tapşırıq tamamlanması
    TaskComment,       // Yeni şərh
    ProjectInvite,     // Layihə dəvəti
    DailyDigest,       // Günlük xülasə
    WeeklyReport,      // Həftəlik hesabat
    Notification,      // Ümumi bildiriş
    SystemAlert        // Sistem xəbərdarlığı
}

public enum EmailStatus
{
    Pending,    // Gözləmədə
    Queued,     // Növbədə
    Sending,    // Göndərilir
    Sent,       // Göndərildi
    Delivered,  // Çatdırıldı
    Opened,     // Açıldı
    Clicked,    // Klikləndi
    Bounced,    // Qayıtdı
    Failed,     // Uğursuz
    Cancelled   // Ləğv edildi
}

/// <summary>
/// İstifadəçi email seçimləri
/// </summary>
public class UserEmailPreference
{
    public long UserId { get; set; }
    
    /// <summary>
    /// Email bildirişləri aktivdir?
    /// </summary>
    public bool EmailNotificationsEnabled { get; set; } = true;
    
    /// <summary>
    /// Günlük xülasə göndərilsin?
    /// </summary>
    public bool DailyDigestEnabled { get; set; } = true;
    
    /// <summary>
    /// Həftəlik hesabat göndərilsin?
    /// </summary>
    public bool WeeklyReportEnabled { get; set; } = true;
    
    /// <summary>
    /// Tapşırıq təyinatı bildirişləri
    /// </summary>
    public bool TaskAssignmentNotifications { get; set; } = true;
    
    /// <summary>
    /// Son tarix (deadline) xəbərdarlıqları
    /// </summary>
    public bool DeadlineReminders { get; set; } = true;
    
    /// <summary>
    /// Şərh bildirişləri
    /// </summary>
    public bool CommentNotifications { get; set; } = true;
    
    /// <summary>
    /// Sistem xəbərdarlıqları
    /// </summary>
    public bool SystemAlerts { get; set; } = true;
    
    /// <summary>
    /// Səssiz saatlar başlanğıc
    /// </summary>
    public TimeSpan? QuietHoursStart { get; set; }
    
    /// <summary>
    /// Səssiz saatlar bitiş
    /// </summary>
    public TimeSpan? QuietHoursEnd { get; set; }
    
    /// <summary>
    /// Həftəsonu bildirişlər
    /// </summary>
    public bool WeekendNotifications { get; set; } = false;
    
    /// <summary>
    /// Son yenilənmə
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    public User User { get; set; } = null!;
}
