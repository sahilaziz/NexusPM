namespace Nexus.Domain.Entities;

/// <summary>
/// Tapşırıq etiketi (Label/Tag)
/// Məsələn: Bug, Feature, Urgent, Design, Backend, Frontend
/// </summary>
public class TaskLabel
{
    public long LabelId { get; set; }
    
    /// <summary>
    /// Etiket adı (unique within project)
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Etiket təsviri
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Rəng kodu (hex: #FF5733)
    /// </summary>
    public string Color { get; set; } = "#6B7280"; // Default gray
    
    /// <summary>
    /// Sıralama prioriteti
    /// </summary>
    public int SortOrder { get; set; } = 0;
    
    /// <summary>
    /// Hansı layihəyə aid (null = global/system label)
    /// </summary>
    public long? ProjectId { get; set; }
    
    /// <summary>
    /// Təşkilat kodu
    /// </summary>
    public string OrganizationCode { get; set; } = "default";
    
    /// <summary>
    /// Sistem etiketidir? (silinə bilməz)
    /// </summary>
    public bool IsSystem { get; set; } = false;
    
    /// <summary>
    /// Aktivdir?
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Yaradılma tarixi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Kim yaratdı
    /// </summary>
    public string CreatedBy { get; set; } = "system";

    // Navigation properties
    public Project? Project { get; set; }
    public ICollection<TaskItemLabel> TaskLabels { get; set; } = new List<TaskItemLabel>();
}

/// <summary>
/// Tapşırıq-Etiket əlaqəsi (Many-to-Many junction table)
/// </summary>
public class TaskItemLabel
{
    public long TaskId { get; set; }
    public long LabelId { get; set; }
    
    /// <summary>
    /// Etiketin tapşırığa təyin edilmə tarixi
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Kim təyin etdi
    /// </summary>
    public string AssignedBy { get; set; } = "system";

    // Navigation properties
    public TaskItem Task { get; set; } = null!;
    public TaskLabel Label { get; set; } = null!;
}

/// <summary>
/// Label ilə birlikdə tapşırıq məlumatı (read model)
/// </summary>
public class TaskWithLabels
{
    public long TaskId { get; set; }
    public string TaskTitle { get; set; } = null!;
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public IReadOnlyList<LabelDto> Labels { get; set; } = new List<LabelDto>();
}

public class LabelDto
{
    public long LabelId { get; set; }
    public string Name { get; set; } = null!;
    public string Color { get; set; } = null!;
}
