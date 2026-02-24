namespace Nexus.Domain.Entities;

/// <summary>
/// Vaxt qeydi (Time Tracking)
/// İstifadəçinin tapşırıq üzərində işlədiyi vaxtın qeydi
/// </summary>
public class TimeEntry
{
    public long TimeEntryId { get; set; }
    
    /// <summary>
    /// Hansı tapşırıq
    /// </summary>
    public long TaskId { get; set; }
    
    /// <summary>
    /// Kim işləyib
    /// </summary>
    public long UserId { get; set; }
    
    /// <summary>
    /// Başlama vaxtı
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Bitmə vaxtı (null = hələ işlənir)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// İşlənən dəqiqələrin hesablanmış dəyəri
    /// </summary>
    public int? DurationMinutes { get; set; }
    
    /// <summary>
    /// Təsvir (nə edilib)
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Əlavə qeydlər
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// İşin tipi (Development, Meeting, Research, vs.)
    /// </summary>
    public WorkType WorkType { get; set; } = WorkType.Development;
    
    /// <summary>
    /// Hesab-fakturaya əlavə edilə bilərmi?
    /// </summary>
    public bool IsBillable { get; set; } = true;
    
    /// <summary>
    /// Saat dərəcəsi (məbləğ hesablamaq üçün)
    /// </summary>
    public decimal? HourlyRate { get; set; }
    
    /// <summary>
    /// Hesablanmış məbləğ
    /// </summary>
    public decimal? CalculatedAmount { get; set; }
    
    /// <summary>
    /// Təsdiqlənibmi (manager tərəfindən)
    /// </summary>
    public bool IsApproved { get; set; } = false;
    
    /// <summary>
    /// Kim təsdiqləyib
    /// </summary>
    public string? ApprovedBy { get; set; }
    
    /// <summary>
    /// Təsdiq tarixi
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
    
    /// <summary>
    /// Redaktə edilibmi?
    /// </summary>
    public bool IsEdited { get; set; } = false;
    
    /// <summary>
    /// Orijinal dəyər (redaktə olunarsa)
    /// </summary>
    public int? OriginalDurationMinutes { get; set; }
    
    /// <summary>
    /// Yaradılma tarixi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Son redaktə tarixi
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    // Navigation properties
    public TaskItem Task { get; set; } = null!;
    public User User { get; set; } = null!;

    /// <summary>
    /// Duration hesabla (dəqiqə ilə)
    /// </summary>
    public void CalculateDuration()
    {
        if (EndTime.HasValue)
        {
            DurationMinutes = (int)(EndTime.Value - StartTime).TotalMinutes;
            
            // Calculate amount if hourly rate is set
            if (HourlyRate.HasValue && IsBillable)
            {
                CalculatedAmount = (DurationMinutes.Value / 60m) * HourlyRate.Value;
            }
        }
    }

    /// <summary>
    /// Hal-hazırda işlənilən vaxtı al (canlı timer üçün)
    /// </summary>
    public TimeSpan GetCurrentDuration()
    {
        if (EndTime.HasValue)
        {
            return EndTime.Value - StartTime;
        }
        return DateTime.UtcNow - StartTime;
    }

    /// <summary>
    /// Formatlı duration göstər
    /// </summary>
    public string GetFormattedDuration()
    {
        var duration = DurationMinutes.HasValue 
            ? TimeSpan.FromMinutes(DurationMinutes.Value)
            : GetCurrentDuration();
        
        if (duration.TotalHours >= 1)
        {
            return $"{duration.Hours}h {duration.Minutes}m";
        }
        return $"{duration.Minutes}m";
    }
}

/// <summary>
/// İş tipi
/// </summary>
public enum WorkType
{
    Development,
    Design,
    Testing,
    Documentation,
    Meeting,
    Research,
    BugFix,
    CodeReview,
    Deployment,
    Maintenance,
    Training,
    Other
}

/// <summary>
/// Günlük vaxt özəti (read model)
/// </summary>
public class DailyTimeSummary
{
    public DateTime Date { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public int TotalMinutes { get; set; }
    public decimal? TotalAmount { get; set; }
    public int EntryCount { get; set; }
    public List<TimeEntrySummary> Entries { get; set; } = new();
}

public class TimeEntrySummary
{
    public long TimeEntryId { get; set; }
    public long TaskId { get; set; }
    public string TaskTitle { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Description { get; set; }
    public WorkType WorkType { get; set; }
    public bool IsBillable { get; set; }
    public bool IsRunning { get; set; }
}

/// <summary>
/// Həftəlik vaxt özəti
/// </summary>
public class WeeklyTimeSummary
{
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public int TotalMinutes { get; set; }
    public decimal? BillableAmount { get; set; }
    public List<DailyBreakdown> DailyBreakdown { get; set; } = new();
}

public class DailyBreakdown
{
    public DateTime Date { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int TotalMinutes { get; set; }
    public int EntryCount { get; set; }
}
