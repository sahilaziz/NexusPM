using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentFTP;
using Nexus.Application.Interfaces.Services;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Storage;

/// <summary>
/// FTP storage servisi
/// </summary>
public class FtpStorageService : IStorageService
{
    private readonly StorageSettings _settings;
    private readonly ILogger<FtpStorageService> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _basePath;
    private readonly bool _useSsl;

    public StorageType StorageType => StorageType.FtpServer;

    public FtpStorageService(
        StorageSettings settings,
        ILogger<FtpStorageService> logger)
    {
        _settings = settings;
        _logger = logger;
        
        var config = settings.Configuration;
        _host = config.FtpHost ?? throw new ArgumentException("FtpHost is required");
        _port = config.FtpPort ?? 21;
        _username = config.FtpUsername ?? throw new ArgumentException("FtpUsername is required");
        _password = config.FtpPassword ?? throw new ArgumentException("FtpPassword is required");
        _basePath = config.FtpBasePath ?? "/";
        _useSsl = config.FtpUseSsl ?? false;
    }

    private AsyncFtpClient CreateClient()
    {
        var client = new AsyncFtpClient(_host, _username, _password, _port);
        client.Config.EncryptionMode = _useSsl ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None;
        client.Config.ValidateAnyCertificate = true;
        return client;
    }

    public async Task<StorageResult> UploadAsync(
        Stream fileStream, 
        string fileName, 
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        try
        {
            await client.Connect(cancellationToken);

            var remotePath = $"{_basePath}/{destinationPath}/{fileName}".Replace("//", "/");
            var remoteDir = Path.GetDirectoryName(remotePath)?.Replace("\\", "/") ?? _basePath;

            // Qovluq yarat (əgər yoxdursa)
            if (!await client.DirectoryExists(remoteDir, cancellationToken))
            {
                await client.CreateDirectory(remoteDir, cancellationToken);
            }

            // Checksum hesabla
            fileStream.Position = 0;
            using (var md5 = MD5.Create())
            {
                var hash = await md5.ComputeHashAsync(fileStream, cancellationToken);
                var checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                fileStream.Position = 0;
            }

            // Fayl yüklə
            var status = await client.UploadStream(fileStream, remotePath, FtpRemoteExists.Overwrite, true, cancellationToken);
            
            if (status != FtpStatus.Success)
            {
                throw new Exception($"FTP upload failed with status: {status}");
            }

            // Fayl ölçüsünü al
            var size = await client.GetFileSize(remotePath, cancellationToken);

            _logger.LogInformation(
                "File uploaded to FTP: {RemotePath}, Size: {Size} bytes",
                remotePath, size);

            return new StorageResult
            {
                Success = true,
                StoragePath = $"{destinationPath}/{fileName}".Replace("//", "/"),
                FileSize = size,
                PublicUrl = $"ftp://{_host}:{_port}{remotePath}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to FTP: {FileName}", fileName);
            return new StorageResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            await client.Disconnect(cancellationToken);
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

    public async Task<Stream> DownloadAsync(
        string storagePath, 
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        try
        {
            await client.Connect(cancellationToken);
            
            var remotePath = $"{_basePath}/{storagePath}".Replace("//", "/");
            var memoryStream = new MemoryStream();
            
            var success = await client.DownloadStream(memoryStream, remotePath, cancellationToken);
            
            if (!success)
            {
                throw new FileNotFoundException($"File not found on FTP: {storagePath}");
            }
            
            memoryStream.Position = 0;
            return memoryStream;
        }
        finally
        {
            await client.Disconnect(cancellationToken);
        }
    }

    public async Task<bool> DeleteAsync(
        string storagePath, 
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        try
        {
            await client.Connect(cancellationToken);
            
            var remotePath = $"{_basePath}/{storagePath}".Replace("//", "/");
            await client.DeleteFile(remotePath, cancellationToken);
            
            _logger.LogInformation("File deleted from FTP: {RemotePath}", remotePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from FTP: {Path}", storagePath);
            return false;
        }
        finally
        {
            await client.Disconnect(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(
        string storagePath, 
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        try
        {
            await client.Connect(cancellationToken);
            
            var remotePath = $"{_basePath}/{storagePath}".Replace("//", "/");
            return await client.FileExists(remotePath, cancellationToken);
        }
        finally
        {
            await client.Disconnect(cancellationToken);
        }
    }

    public Task<string?> GetPublicUrlAsync(
        string storagePath, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // FTP üçün public URL yoxdur (ftp:// protokolu ilə əlçatandır)
        var remotePath = $"{_basePath}/{storagePath}".Replace("//", "/");
        return Task.FromResult<string?>($"ftp://{_host}:{_port}{remotePath}");
    }

    public async Task<StorageHealthCheckResult> HealthCheckAsync(
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        try
        {
            await client.Connect(cancellationToken);
            
            // Disk məlumatlarını al (əgər server dəstəkləyirsə)
            long? availableSpace = null;
            try
            {
                // Bu bütün FTP serverlərdə işləməyə bilər
                // Sadəcə bağlantı yoxlanışı kifayətdir
            }
            catch { }

            return new StorageHealthCheckResult
            {
                IsHealthy = true,
                Message = $"FTP server is accessible: {_host}:{_port}",
                AvailableSpace = availableSpace,
                Details = new Dictionary<string, object>
                {
                    ["Host"] = _host,
                    ["Port"] = _port,
                    ["BasePath"] = _basePath,
                    ["UseSsl"] = _useSsl
                }
            };
        }
        catch (Exception ex)
        {
            return new StorageHealthCheckResult
            {
                IsHealthy = false,
                Message = $"FTP connection failed: {ex.Message}"
            };
        }
        finally
        {
            await client.Disconnect(cancellationToken);
        }
    }

    public async Task<bool> CreateDirectoryAsync(
        string path, 
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        try
        {
            await client.Connect(cancellationToken);
            
            var remotePath = $"{_basePath}/{path}".Replace("//", "/");
            await client.CreateDirectory(remotePath, cancellationToken);
            
            _logger.LogInformation("Directory created on FTP: {RemotePath}", remotePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory on FTP: {Path}", path);
            return false;
        }
        finally
        {
            await client.Disconnect(cancellationToken);
        }
    }
}
