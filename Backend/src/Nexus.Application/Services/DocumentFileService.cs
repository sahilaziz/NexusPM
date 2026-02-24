using Nexus.Application.Interfaces.Repositories;
using Nexus.Application.Interfaces.Services;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services;

/// <summary>
/// Sənəd fayl idarəetmə servisi
/// </summary>
public interface IDocumentFileService
{
    /// <summary>
    /// Sənəd faylını yüklə
    /// </summary>
    Task<StoredFileResult> UploadFileAsync(
        long documentId,
        Stream fileStream,
        string fileName,
        string contentType,
        string uploadedBy,
        int? storageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sənəd faylını endir
    /// </summary>
    Task<Stream> DownloadFileAsync(
        long fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sənədin bütün fayllarını əldə et
    /// </summary>
    Task<IEnumerable<StoredFileInfo>> GetDocumentFilesAsync(
        long documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fayl sil
    /// </summary>
    Task<bool> DeleteFileAsync(
        long fileId,
        string deletedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Birbaşa download URL əldə et (əgər storage dəstəkləyirsə)
    /// </summary>
    Task<string?> GetDirectDownloadUrlAsync(
        long fileId,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Storage health check
    /// </summary>
    Task<IEnumerable<StorageHealthInfo>> GetStorageHealthAsync(
        CancellationToken cancellationToken = default);
}

public class DocumentFileService : IDocumentFileService
{
    private readonly IStorageFactory _storageFactory;
    private readonly IDocumentRepository _documentRepository;
    private readonly IStorageFileRepository _fileRepository;
    private readonly ILogger<DocumentFileService> _logger;

    public DocumentFileService(
        IStorageFactory storageFactory,
        IDocumentRepository documentRepository,
        IStorageFileRepository fileRepository,
        ILogger<DocumentFileService> logger)
    {
        _storageFactory = storageFactory;
        _documentRepository = documentRepository;
        _fileRepository = fileRepository;
        _logger = logger;
    }

    public async Task<StoredFileResult> UploadFileAsync(
        long documentId,
        Stream fileStream,
        string fileName,
        string contentType,
        string uploadedBy,
        int? storageId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Sənədi tap
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                throw new InvalidOperationException($"Document not found: {documentId}");
            }

            // Storage seç
            IStorageService storage;
            if (storageId.HasValue)
            {
                storage = await _storageFactory.GetStorageAsync(storageId.Value);
            }
            else
            {
                storage = await _storageFactory.GetDefaultStorageAsync();
            }

            // Destination path qur
            var destinationPath = BuildStoragePath(document);

            // Fayl adını təhlükəsiz et
            var safeFileName = MakeSafeFileName(fileName);

            // Faylı yüklə
            var result = await storage.UploadAsync(
                fileStream, 
                safeFileName, 
                destinationPath,
                cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to upload file: {result.ErrorMessage}");
            }

            // Database-ə yaz
            var storedFile = new StoredFileInfo
            {
                DocumentId = documentId,
                StorageId = storageId ?? await GetDefaultStorageIdAsync(),
                OriginalFileName = fileName,
                StoragePath = result.StoragePath,
                ExternalFileId = result.ExternalFileId,
                PublicUrl = result.PublicUrl,
                FileSize = result.FileSize,
                MimeType = contentType,
                Checksum = result.Checksum,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = uploadedBy
            };

            var fileId = await _fileRepository.CreateAsync(storedFile);

            _logger.LogInformation(
                "File uploaded for document {DocumentId}: {FileName}, Size: {Size} bytes, Storage: {StorageId}",
                documentId, fileName, result.FileSize, storageId);

            return new StoredFileResult
            {
                Success = true,
                FileId = fileId,
                FileName = fileName,
                FileSize = result.FileSize,
                StoragePath = result.StoragePath,
                PublicUrl = result.PublicUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file for document {DocumentId}: {FileName}", 
                documentId, fileName);
            return new StoredFileResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<Stream> DownloadFileAsync(
        long fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null || file.IsDeleted)
        {
            throw new FileNotFoundException($"File not found: {fileId}");
        }

        var storage = await _storageFactory.GetStorageAsync(file.StorageId);
        return await storage.DownloadAsync(file.StoragePath, cancellationToken);
    }

    public async Task<IEnumerable<StoredFileInfo>> GetDocumentFilesAsync(
        long documentId,
        CancellationToken cancellationToken = default)
    {
        return await _fileRepository.GetByDocumentIdAsync(documentId);
    }

    public async Task<bool> DeleteFileAsync(
        long fileId,
        string deletedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.IsDeleted)
            {
                return false;
            }

            // Storage-dən sil
            var storage = await _storageFactory.GetStorageAsync(file.StorageId);
            var deleted = await storage.DeleteAsync(file.StoragePath, cancellationToken);

            // Database-də qeyd et
            if (deleted)
            {
                await _fileRepository.MarkAsDeletedAsync(fileId, deletedBy);
                _logger.LogInformation("File {FileId} deleted by {DeletedBy}", fileId, deletedBy);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileId}", fileId);
            return false;
        }
    }

    public async Task<string?> GetDirectDownloadUrlAsync(
        long fileId,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null || file.IsDeleted)
        {
            return null;
        }

        // Əgər artıq public URL varsa
        if (!string.IsNullOrEmpty(file.PublicUrl))
        {
            return file.PublicUrl;
        }

        // Storage-dən URL əldə et
        var storage = await _storageFactory.GetStorageAsync(file.StorageId);
        var url = await storage.GetPublicUrlAsync(file.StoragePath, expiration, cancellationToken);

        // URL-i cache-lə
        if (!string.IsNullOrEmpty(url))
        {
            await _fileRepository.UpdatePublicUrlAsync(fileId, url);
        }

        return url;
    }

    public async Task<IEnumerable<StorageHealthInfo>> GetStorageHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var storages = await _storageFactory.GetAllActiveStoragesAsync();
        var results = new List<StorageHealthInfo>();

        foreach (var storage in storages)
        {
            try
            {
                var health = await storage.HealthCheckAsync(cancellationToken);
                results.Add(new StorageHealthInfo
                {
                    StorageType = storage.StorageType,
                    IsHealthy = health.IsHealthy,
                    Message = health.Message,
                    AvailableSpace = health.AvailableSpace,
                    TotalSpace = health.TotalSpace
                });
            }
            catch (Exception ex)
            {
                results.Add(new StorageHealthInfo
                {
                    StorageType = storage.StorageType,
                    IsHealthy = false,
                    Message = $"Health check failed: {ex.Message}"
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Storage path qur: {İDARƏ}/{QUYU}/{MƏNTƏQƏ}/
    /// </summary>
    private string BuildStoragePath(DocumentNode document)
    {
        // Materialized path-dən hierarchy çıxar
        // /1/5/12/25/ → parts = [1, 5, 12, 25]
        var parts = document.MaterializedPath?.Trim('/').Split('/');
        if (parts == null || parts.Length < 2)
        {
            return "uncategorized";
        }

        // Sonuncu node ID-sini çıxar (özu)
        var pathParts = parts.Take(parts.Length - 1).ToList();
        
        // TODO: Burada hər bir node ID-sini adına çevirmək lazımdır
        // Hazırca sadəcə path qaytarırıq
        return string.Join("/", pathParts);
    }

    private string MakeSafeFileName(string fileName)
    {
        // Fayl adını təhlükəsiz et
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(fileName
            .Select(c => invalid.Contains(c) ? '_' : c)
            .ToArray());

        // Uzunluğu məhdudlaşdır
        if (safe.Length > 200)
        {
            var ext = Path.GetExtension(safe);
            safe = safe.Substring(0, 200 - ext.Length) + ext;
        }

        return safe;
    }

    private async Task<int> GetDefaultStorageIdAsync()
    {
        // TODO: Implement this using IStorageSettingsRepository
        return 1;
    }
}

public class StoredFileResult
{
    public bool Success { get; set; }
    public long FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? PublicUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class StoredFileInfo
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

public class StorageHealthInfo
{
    public StorageType StorageType { get; set; }
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public long? AvailableSpace { get; set; }
    public long? TotalSpace { get; set; }
}

public interface IStorageFileRepository
{
    Task<long> CreateAsync(StoredFileInfo file);
    Task<StoredFileInfo?> GetByIdAsync(long fileId);
    Task<IEnumerable<StoredFileInfo>> GetByDocumentIdAsync(long documentId);
    Task MarkAsDeletedAsync(long fileId, string deletedBy);
    Task UpdatePublicUrlAsync(long fileId, string publicUrl);
}
