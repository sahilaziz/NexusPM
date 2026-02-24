namespace Nexus.Infrastructure.Repositories;

/// <summary>
/// Stored file database entity
/// </summary>
public class StoredFileEntity
{
    public long FileId { get; set; }
    public long DocumentId { get; set; }
    public int StorageId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ExternalFileId { get; set; }
    public string? PublicUrl { get; set; }
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? Checksum { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
