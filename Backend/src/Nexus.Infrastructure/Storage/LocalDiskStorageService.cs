using System.Security.Cryptography;
using Nexus.Application.Interfaces.Services;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Storage;

/// <summary>
/// Yerli disk storage servisi (D:, E: və s.)
/// </summary>
public class LocalDiskStorageService : IStorageService
{
    private readonly StorageSettings _settings;
    private readonly ILogger<LocalDiskStorageService> _logger;
    private readonly string _basePath;

    public StorageType StorageType => StorageType.LocalDisk;

    public LocalDiskStorageService(
        StorageSettings settings,
        ILogger<LocalDiskStorageService> logger)
    {
        _settings = settings;
        _logger = logger;
        _basePath = settings.Configuration.BasePath 
            ?? throw new ArgumentException("BasePath is required for LocalDisk storage");
        
        // Base path mövcudluğunu yoxla və yarat
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created base directory: {BasePath}", _basePath);
        }
    }

    public async Task<StorageResult> UploadAsync(
        Stream fileStream, 
        string fileName, 
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, destinationPath, fileName);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Checksum hesabla
            string checksum;
            using (var md5 = MD5.Create())
            {
                var hash = await md5.ComputeHashAsync(fileStream, cancellationToken);
                checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                fileStream.Position = 0;
            }

            // Fayl yaz
            await using (var fileStreamDest = new FileStream(
                fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                await fileStream.CopyToAsync(fileStreamDest, cancellationToken);
            }

            var fileInfo = new FileInfo(fullPath);

            _logger.LogInformation(
                "File uploaded to local disk: {Path}, Size: {Size} bytes",
                fullPath, fileInfo.Length);

            return new StorageResult
            {
                Success = true,
                StoragePath = Path.Combine(destinationPath, fileName).Replace("\\", "/"),
                FileSize = fileInfo.Length,
                Checksum = checksum
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to local disk: {FileName}", fileName);
            return new StorageResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<StorageResult> UploadAsync(
        byte[] fileData, 
        string fileName, 
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(fileData);
        return await UploadAsync(stream, fileName, destinationPath, cancellationToken);
    }

    public Task<Stream> DownloadAsync(
        string storagePath, 
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {storagePath}");
        }

        var stream = new FileStream(
            fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        
        return Task.FromResult<Stream>(stream);
    }

    public Task<bool> DeleteAsync(
        string storagePath, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {Path}", fullPath);
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Path}", storagePath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ExistsAsync(
        string storagePath, 
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<string?> GetPublicUrlAsync(
        string storagePath, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // Local disk üçün public URL yoxdur
        return Task.FromResult<string?>(null);
    }

    public Task<StorageHealthCheckResult> HealthCheckAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(_basePath) ?? _basePath);
            
            return Task.FromResult(new StorageHealthCheckResult
            {
                IsHealthy = true,
                Message = $"Local disk is accessible: {_basePath}",
                AvailableSpace = driveInfo.AvailableFreeSpace,
                TotalSpace = driveInfo.TotalSize,
                Details = new Dictionary<string, object>
                {
                    ["DriveFormat"] = driveInfo.DriveFormat,
                    ["DriveType"] = driveInfo.DriveType.ToString(),
                    ["VolumeLabel"] = driveInfo.VolumeLabel ?? "Unknown"
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new StorageHealthCheckResult
            {
                IsHealthy = false,
                Message = $"Local disk health check failed: {ex.Message}"
            });
        }
    }

    public Task<bool> CreateDirectoryAsync(
        string path, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, path);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                _logger.LogInformation("Directory created: {Path}", fullPath);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory: {Path}", path);
            return Task.FromResult(false);
        }
    }
}
