namespace Nexus.Domain.Entities;

/// <summary>
/// Tapşırıq (Task) - System.Threading.Task ilə qarışmaması üçün TaskItem
/// </summary>
public class TaskItem
{
    public long TaskId { get; set; }
    public long ProjectId { get; set; }
    public long? ParentTaskId { get; set; }
    
    public string TaskTitle { get; set; } = null!;
    public string? TaskDescription { get; set; }
    
    // Assignment
    public string? AssignedTo { get; set; }
    public string CreatedBy { get; set; } = "system";
    
    // Status və prioritet
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    
    // Tarixlər
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Əlaqəli sənəd (məktub və s.)
    /// </summary>
    public long? DocumentNodeId { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    
    // Navigation
    public Project? Project { get; set; }
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();
    public DocumentNode? DocumentNode { get; set; }
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}

public enum TaskStatus
{
    Todo,
    InProgress,
    Review,
    Done,
    Cancelled
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}
