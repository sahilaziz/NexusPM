using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Interfaces.Services;
using Nexus.Application.Services;
using Nexus.Domain.Entities;

namespace Nexus.API.Controllers;

/// <summary>
/// Storage idarəetmə (Admin üçün)
/// </summary>
[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class StorageController : ControllerBase
{
    private readonly IStorageSettingsRepository _storageRepository;
    private readonly IDocumentFileService _documentFileService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(
        IStorageSettingsRepository storageRepository,
        IDocumentFileService documentFileService,
        ILogger<StorageController> logger)
    {
        _storageRepository = storageRepository;
        _documentFileService = documentFileService;
        _logger = logger;
    }

    /// <summary>
    /// Bütün storage-ləri əldə et
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var storages = await _storageRepository.GetAllActiveAsync();
        return Ok(new { Success = true, Data = storages });
    }

    /// <summary>
    /// Storage detalları
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var storage = await _storageRepository.GetByIdAsync(id);
        if (storage == null)
            return NotFound(new { Success = false, Message = "Storage not found" });

        return Ok(new { Success = true, Data = storage });
    }

    /// <summary>
    /// Yeni local disk storage əlavə et
    /// </summary>
    [HttpPost("local-disk")]
    public async Task<ActionResult> AddLocalDisk([FromBody] AddLocalDiskRequest request)
    {
        try
        {
            // Path-in mövcud olduğunu yoxla
            if (!Directory.Exists(request.BasePath))
            {
                try
                {
                    Directory.CreateDirectory(request.BasePath);
                }
                catch (Exception ex)
                {
                    return BadRequest(new 
                    { 
                        Success = false, 
                        Message = $"Cannot create directory: {ex.Message}" 
                    });
                }
            }

            var settings = new StorageSettings
            {
                StorageName = request.StorageName,
                Type = StorageType.LocalDisk,
                IsDefault = request.IsDefault,
                Configuration = new StorageConfiguration
                {
                    BasePath = request.BasePath
                },
                CreatedBy = User.Identity?.Name ?? "system"
            };

            var result = await _storageRepository.CreateAsync(settings);

            _logger.LogInformation(
                "Local disk storage created: {StorageName}, Path: {Path}",
                request.StorageName, request.BasePath);

            return Ok(new { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Yeni FTP storage əlavə et
    /// </summary>
    [HttpPost("ftp")]
    public async Task<ActionResult> AddFtp([FromBody] AddFtpRequest request)
    {
        try
        {
            var settings = new StorageSettings
            {
                StorageName = request.StorageName,
                Type = StorageType.FtpServer,
                IsDefault = request.IsDefault,
                Configuration = new StorageConfiguration
                {
                    FtpHost = request.Host,
                    FtpPort = request.Port ?? 21,
                    FtpUsername = request.Username,
                    FtpPassword = request.Password,
                    FtpBasePath = request.BasePath ?? "/",
                    FtpUseSsl = request.UseSsl ?? false
                },
                CreatedBy = User.Identity?.Name ?? "system"
            };

            var result = await _storageRepository.CreateAsync(settings);

            _logger.LogInformation(
                "FTP storage created: {StorageName}, Host: {Host}",
                request.StorageName, request.Host);

            return Ok(new { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Yeni OneDrive storage əlavə et
    /// </summary>
    [HttpPost("onedrive")]
    public async Task<ActionResult> AddOneDrive([FromBody] AddOneDriveRequest request)
    {
        try
        {
            var settings = new StorageSettings
            {
                StorageName = request.StorageName,
                Type = StorageType.OneDrive,
                IsDefault = request.IsDefault,
                Configuration = new StorageConfiguration
                {
                    OneDriveClientId = request.ClientId,
                    OneDriveClientSecret = request.ClientSecret,
                    OneDriveTenantId = request.TenantId ?? "common",
                    OneDriveFolderId = request.FolderId,
                    OneDriveRefreshToken = request.RefreshToken
                },
                CreatedBy = User.Identity?.Name ?? "system"
            };

            var result = await _storageRepository.CreateAsync(settings);

            _logger.LogInformation(
                "OneDrive storage created: {StorageName}",
                request.StorageName);

            return Ok(new { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Storage sil (deaktiv et)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await _storageRepository.DeleteAsync(id);
        _logger.LogInformation("Storage {StorageId} deleted", id);
        return Ok(new { Success = true, Message = "Storage deleted" });
    }

    /// <summary>
    /// Default storage təyin et
    /// </summary>
    [HttpPost("{id}/set-default")]
    public async Task<ActionResult> SetDefault(int id)
    {
        await _storageRepository.SetDefaultAsync(id);
        _logger.LogInformation("Storage {StorageId} set as default", id);
        return Ok(new { Success = true, Message = "Default storage updated" });
    }

    /// <summary>
    /// Storage health check
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult> HealthCheck()
    {
        var health = await _documentFileService.GetStorageHealthAsync();
        return Ok(new { Success = true, Data = health });
    }

    /// <summary>
    /// Storage tiplərini əldə et
    /// </summary>
    [HttpGet("types")]
    [AllowAnonymous]
    public ActionResult GetTypes()
    {
        var types = Enum.GetValues<StorageType>();
        return Ok(new 
        { 
            Success = true, 
            Data = types.Select(t => new 
            { 
                Type = t.ToString(),
                DisplayName = GetStorageTypeDisplayName(t)
            })
        });
    }

    private string GetStorageTypeDisplayName(StorageType type)
    {
        return type switch
        {
            StorageType.LocalDisk => "Yerli Disk (D:, E: və s.)",
            StorageType.FtpServer => "FTP Server",
            StorageType.OneDrive => "Microsoft OneDrive",
            StorageType.GoogleDrive => "Google Drive",
            StorageType.NetworkShare => "Şəbəkə Yolu (\\\\Server\\Share)",
            _ => type.ToString()
        };
    }
}

// DTOs
public class AddLocalDiskRequest
{
    public string StorageName { get; set; } = string.Empty;
    public string BasePath { get; set; } = string.Empty;  // Məs: "D:\\NexusStorage" və ya "\\\Server\Storage"
    public bool IsDefault { get; set; }
}

public class AddFtpRequest
{
    public string StorageName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int? Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? BasePath { get; set; }
    public bool? UseSsl { get; set; }
    public bool IsDefault { get; set; }
}

public class AddOneDriveRequest
{
    public string StorageName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string? FolderId { get; set; }
    public string? RefreshToken { get; set; }
    public bool IsDefault { get; set; }
}
