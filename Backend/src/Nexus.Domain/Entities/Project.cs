namespace Nexus.Domain.Entities;

/// <summary>
/// Layihə (Project)
/// </summary>
public class Project
{
    public long ProjectId { get; set; }
    
    /// <summary>
    /// Unikal layihə kodu: PRJ-2024-001
    /// </summary>
    public string ProjectCode { get; set; } = null!;
    
    public string ProjectName { get; set; } = null!;
    public string? Description { get; set; }
    
    /// <summary>
    /// Təşkilat kodu (təcrid üçün)
    /// </summary>
    public string OrganizationCode { get; set; } = "default";
    
    /// <summary>
    /// Əlaqəli sənəd qovluğu (məktublar üçün)
    /// </summary>
    public long? DocumentNodeId { get; set; }
    
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    
    // Navigation
    public DocumentNode? DocumentNode { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<UserProjectRole> UserRoles { get; set; } = new List<UserProjectRole>();
}

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Cancelled
}
