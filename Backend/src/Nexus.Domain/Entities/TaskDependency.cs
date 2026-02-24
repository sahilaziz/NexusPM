namespace Nexus.Domain.Entities;

/// <summary>
/// Tapşırıq asılılıqları (Task Dependencies)
/// Bir tapşırıq digərinin tamamlanmasını/gozləməsini tələb edə bilər
/// </summary>
public class TaskDependency
{
    public long DependencyId { get; set; }
    
    /// <summary>
    /// Asılı olan tapşırıq (bu tapşırıq digərindən asılıdır)
    /// </summary>
    public long TaskId { get; set; }
    
    /// <summary>
    /// Əsas tapşırıq (bu tapşırıq tamamlanmalıdır)
    /// </summary>
    public long DependsOnTaskId { get; set; }
    
    /// <summary>
    /// Asılılıq tipi
    /// </summary>
    public DependencyType Type { get; set; } = DependencyType.FinishToStart;
    
    /// <summary>
    /// Lag time (gün ilə) - misal: 2 gün gözləmə
    /// </summary>
    public int LagDays { get; set; } = 0;
    
    /// <summary>
    /// Opsiyonel izah
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Yaradılma tarixi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Kim yaratdı
    /// </summary>
    public string CreatedBy { get; set; } = "system";

    // Navigation properties
    public TaskItem Task { get; set; } = null!;
    public TaskItem DependsOnTask { get; set; } = null!;
}

/// <summary>
/// Asılılıq tipləri (PMBOK standartı)
/// </summary>
public enum DependencyType
{
    /// <summary>
    /// Finish-to-Start (FS): A tapşırığı bitdikdən sonra B başlaya bilər
    /// Ən çox istifadə edilən
    /// </summary>
    FinishToStart,
    
    /// <summary>
    /// Start-to-Start (SS): A tapşırığı başladıqdan sonra B başlaya bilər
    /// </summary>
    StartToStart,
    
    /// <summary>
    /// Finish-to-Finish (FF): A tapşırığı bitdikdən sonra B bitə bilər
    /// </summary>
    FinishToFinish,
    
    /// <summary>
    /// Start-to-Finish (SF): A tapşırığı başladıqdan sonra B bitə bilər
    /// Çox nadir istifadə edilir
    /// </summary>
    StartToFinish
}

/// <summary>
/// Tapşırıq asılılıq məlumatı (read model üçün)
/// </summary>
public class TaskDependencyInfo
{
    public long DependencyId { get; set; }
    public long TaskId { get; set; }
    public string TaskTitle { get; set; } = null!;
    public TaskStatus TaskStatus { get; set; }
    
    public long DependsOnTaskId { get; set; }
    public string DependsOnTaskTitle { get; set; } = null!;
    public TaskStatus DependsOnTaskStatus { get; set; }
    
    public DependencyType Type { get; set; }
    public int LagDays { get; set; }
    public bool IsBlocking => DependsOnTaskStatus != TaskStatus.Done;
}
