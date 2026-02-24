namespace Nexus.Domain.Entities;

/// <summary>
/// Saxlama konfiqurasiyası
/// </summary>
public class StorageSettings
{
    public int StorageId { get; set; }
    public string StorageName { get; set; } = string.Empty;
    public StorageType Type { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public StorageConfiguration Configuration { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

public enum StorageType
{
    LocalDisk,      // Yerli disk (D:, E: və s.)
    FtpServer,      // FTP server
    OneDrive,       // Microsoft OneDrive
    GoogleDrive,    // Google Drive
    NetworkShare    // Şəbəkə yolu (\\Server\Share)
}

/// <summary>
/// Saxlama konfiqurasiyası - JSON olaraq saxlanılacaq
/// </summary>
public class StorageConfiguration
{
    // Local Disk üçün
    public string? BasePath { get; set; }
    
    // FTP üçün
    public string? FtpHost { get; set; }
    public int? FtpPort { get; set; }
    public string? FtpUsername { get; set; }
    public string? FtpPassword { get; set; }
    public string? FtpBasePath { get; set; }
    public bool? FtpUseSsl { get; set; }
    
    // OneDrive üçün
    public string? OneDriveClientId { get; set; }
    public string? OneDriveClientSecret { get; set; }
    public string? OneDriveTenantId { get; set; }
    public string? OneDriveFolderId { get; set; }  // Root folder ID
    public string? OneDriveRefreshToken { get; set; }
    
    // Google Drive üçün
    public string? GoogleDriveClientId { get; set; }
    public string? GoogleDriveClientSecret { get; set; }
    public string? GoogleDriveFolderId { get; set; }
    public string? GoogleDriveRefreshToken { get; set; }
    
    // Network Share üçün
    public string? NetworkPath { get; set; }
    public string? NetworkUsername { get; set; }
    public string? NetworkPassword { get; set; }
    public string? NetworkDomain { get; set; }
}

/// <summary>
/// Saxlanan fayl məlumatı
/// </summary>
public class StoredFile
{
    public long FileId { get; set; }
    public long DocumentId { get; set; }
    public int StorageId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;  // Faylın storage-dəki yolu
    public string? ExternalFileId { get; set; }  // Cloud üçün (OneDrive/Google Drive file ID)
    public string? PublicUrl { get; set; }  // Birbaşa URL (əgər varsa)
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? Checksum { get; set; }  // MD5/SHA256
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    
    public DocumentNode Document { get; set; } = null!;
    public StorageSettings Storage { get; set; } = null!;
}
