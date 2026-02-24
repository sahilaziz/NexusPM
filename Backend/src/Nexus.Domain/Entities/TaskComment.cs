namespace Nexus.Domain.Entities;

public class TaskComment
{
    public long CommentId { get; set; }
    public long TaskId { get; set; }
    public string UserId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public TaskItem Task { get; set; } = null!;
}

public class TaskAttachment
{
    public long AttachmentId { get; set; }
    public long TaskId { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = null!;
    
    public TaskItem Task { get; set; } = null!;
}
