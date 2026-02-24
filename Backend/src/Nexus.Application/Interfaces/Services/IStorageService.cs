using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Services;

/// <summary>
/// Storage service interface - Strategy Pattern
/// </summary>
public interface IStorageService
{
    StorageType StorageType { get; }
    
    /// <summary>
    /// Fayl yüklə
    /// </summary>
    Task<StorageResult> UploadAsync(
        Stream fileStream, 
        string fileName, 
        string destinationPath,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fayl yüklə (byte array)
    /// </summary>
    Task<StorageResult> UploadAsync(
        byte[] fileData, 
        string fileName, 
        string destinationPath,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fayl endir
    /// </summary>
    Task<Stream> DownloadAsync(
        string storagePath, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fayl sil
    /// </summary>
    Task<bool> DeleteAsync(
        string storagePath, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fayl mövcudluğunu yoxla
    /// </summary>
    Task<bool> ExistsAsync(
        string storagePath, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Birbaşa URL al (əgər dəstəklənirsə)
    /// </summary>
    Task<string?> GetPublicUrlAsync(
        string storagePath, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Konfiqurasiyanı yoxla
    /// </summary>
    Task<StorageHealthCheckResult> HealthCheckAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Qovluq yarat
    /// </summary>
    Task<bool> CreateDirectoryAsync(
        string path, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Storage factory - düzgün storage servisini seçir
/// </summary>
public interface IStorageFactory
{
    /// <summary>
    /// Default storage servisi
    /// </summary>
    Task<IStorageService> GetDefaultStorageAsync();
    
    /// <summary>
    /// Storage tipinə görə servis
    /// </summary>
    Task<IStorageService> GetStorageAsync(StorageType type);
    
    /// <summary>
    /// Konfiqurasiyaya görə servis
    /// </summary>
    Task<IStorageService> GetStorageAsync(int storageId);
    
    /// <summary>
    /// Bütün aktiv storage-ləri əldə et
    /// </summary>
    Task<IEnumerable<IStorageService>> GetAllActiveStoragesAsync();
}

/// <summary>
/// Storage əməliyyat nəticəsi
/// </summary>
public class StorageResult
{
    public bool Success { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? ExternalFileId { get; set; }
    public string? PublicUrl { get; set; }
    public long FileSize { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Checksum { get; set; }
}

/// <summary>
/// Storage sağlamlıq yoxlanışı nəticəsi
/// </summary>
public class StorageHealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public long? AvailableSpace { get; set; }  // Byte
    public long? TotalSpace { get; set; }  // Byte
    public Dictionary<string, object>? Details { get; set; }
}
