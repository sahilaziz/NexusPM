using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Nexus.Application.Interfaces.Services;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Storage;

/// <summary>
/// Microsoft OneDrive storage servisi
/// </summary>
public class OneDriveStorageService : IStorageService
{
    private readonly StorageSettings _settings;
    private readonly ILogger<OneDriveStorageService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tenantId;
    private readonly string? _folderId;
    private readonly string? _refreshToken;
    
    private const string GraphApiBaseUrl = "https://graph.microsoft.com/v1.0";
    private const string TokenEndpoint = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

    public StorageType StorageType => StorageType.OneDrive;

    public OneDriveStorageService(
        StorageSettings settings,
        ILogger<OneDriveStorageService> logger,
        IMemoryCache cache)
    {
        _settings = settings;
        _logger = logger;
        _cache = cache;
        
        var config = settings.Configuration;
        _clientId = config.OneDriveClientId ?? throw new ArgumentException("OneDriveClientId is required");
        _clientSecret = config.OneDriveClientSecret ?? throw new ArgumentException("OneDriveClientSecret is required");
        _tenantId = config.OneDriveTenantId ?? "common";
        _folderId = config.OneDriveFolderId;
        _refreshToken = config.OneDriveRefreshToken;
    }

    /// <summary>
    /// Access token əldə et (refresh token ilə)
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Cache-dən yoxla
        if (_cache.TryGetValue($"onedrive_token_{_settings.StorageId}", out string? cachedToken) && 
            !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        if (string.IsNullOrEmpty(_refreshToken))
        {
            throw new InvalidOperationException("Refresh token not configured. Please authenticate first.");
        }

        using var httpClient = new HttpClient();
        var tokenEndpoint = string.Format(TokenEndpoint, _tenantId);
        
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = _refreshToken,
            ["grant_type"] = "refresh_token",
            ["scope"] = "https://graph.microsoft.com/Files.ReadWrite offline_access"
        });

        var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
        
        if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
        {
            throw new InvalidOperationException("Failed to obtain access token");
        }

        // Cache-ə yaz (token expiry - 5 dəqiqə)
        var expiry = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 300);
        _cache.Set($"onedrive_token_{_settings.StorageId}", tokenResponse.AccessToken, expiry);

        return tokenResponse.AccessToken;
    }

    public async Task<StorageResult> UploadAsync(
        Stream fileStream, 
        string fileName, 
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            var uploadUrl = $"{GraphApiBaseUrl}/me/drive/items/{_folderId}:/{destinationPath}/{fileName}:/content";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            // Simple upload (< 4MB)
            var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await httpClient.PutAsync(uploadUrl, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"OneDrive upload failed: {error}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var driveItem = JsonSerializer.Deserialize<DriveItem>(json);

            _logger.LogInformation(
                "File uploaded to OneDrive: {FileName}, ID: {FileId}, Size: {Size}",
                fileName, driveItem?.Id, driveItem?.Size);

            return new StorageResult
            {
                Success = true,
                StoragePath = $"{destinationPath}/{fileName}",
                ExternalFileId = driveItem?.Id,
                FileSize = driveItem?.Size ?? 0,
                PublicUrl = driveItem?.WebUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to OneDrive: {FileName}", fileName);
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

    public async Task<Stream> DownloadAsync(
        string storagePath, 
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        
        // Fayl ID-sini tap
        var fileId = await GetFileIdByPathAsync(storagePath, accessToken, cancellationToken);
        if (string.IsNullOrEmpty(fileId))
        {
            throw new FileNotFoundException($"File not found on OneDrive: {storagePath}");
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Download URL əldə et
        var response = await httpClient.GetAsync(
            $"{GraphApiBaseUrl}/me/drive/items/{fileId}/content?format=pdf",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new FileNotFoundException($"File not found on OneDrive: {storagePath}");
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        string storagePath, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            var fileId = await GetFileIdByPathAsync(storagePath, accessToken, cancellationToken);
            
            if (string.IsNullOrEmpty(fileId))
            {
                return false;
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.DeleteAsync(
                $"{GraphApiBaseUrl}/me/drive/items/{fileId}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("File deleted from OneDrive: {Path}", storagePath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from OneDrive: {Path}", storagePath);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(
        string storagePath, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            var fileId = await GetFileIdByPathAsync(storagePath, accessToken, cancellationToken);
            return !string.IsNullOrEmpty(fileId);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetPublicUrlAsync(
        string storagePath, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            var fileId = await GetFileIdByPathAsync(storagePath, accessToken, cancellationToken);
            
            if (string.IsNullOrEmpty(fileId))
            {
                return null;
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            // Create sharing link
            var requestBody = new
            {
                type = expiration.HasValue ? "view" : "edit",
                scope = "anonymous"
            };

            var response = await httpClient.PostAsJsonAsync(
                $"{GraphApiBaseUrl}/me/drive/items/{fileId}/createLink",
                requestBody,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var linkResponse = JsonSerializer.Deserialize<SharingLinkResponse>(json);
                return linkResponse?.Link?.WebUrl;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get public URL from OneDrive: {Path}", storagePath);
            return null;
        }
    }

    public async Task<StorageHealthCheckResult> HealthCheckAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            // Drive info əldə et
            var response = await httpClient.GetAsync(
                $"{GraphApiBaseUrl}/me/drive",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var drive = JsonSerializer.Deserialize<DriveInfo>(json);

                return new StorageHealthCheckResult
                {
                    IsHealthy = true,
                    Message = "OneDrive connection successful",
                    AvailableSpace = drive?.Quota?.Remaining,
                    TotalSpace = drive?.Quota?.Total,
                    Details = new Dictionary<string, object>
                    {
                        ["DriveType"] = drive?.DriveType ?? "Unknown",
                        ["Owner"] = drive?.Owner?.User?.DisplayName ?? "Unknown"
                    }
                };
            }

            return new StorageHealthCheckResult
            {
                IsHealthy = false,
                Message = $"OneDrive health check failed: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new StorageHealthCheckResult
            {
                IsHealthy = false,
                Message = $"OneDrive connection failed: {ex.Message}"
            };
        }
    }

    public async Task<bool> CreateDirectoryAsync(
        string path, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            // OneDrive-də qovluq yaratma
            var requestBody = new
            {
                name = Path.GetFileName(path),
                folder = new { },
                @microsoft.graph.conflictBehavior = "replace"
            };

            var response = await httpClient.PostAsJsonAsync(
                $"{GraphApiBaseUrl}/me/drive/items/{_folderId}/children",
                requestBody,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Directory created on OneDrive: {Path}", path);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory on OneDrive: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Path-ə görə fayl ID-sini tap
    /// </summary>
    private async Task<string?> GetFileIdByPathAsync(
        string path, 
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            var encodedPath = Uri.EscapeDataString(path);
            var response = await httpClient.GetAsync(
                $"{GraphApiBaseUrl}/me/drive/root:/{encodedPath}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var item = JsonSerializer.Deserialize<DriveItem>(json);
                return item?.Id;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // DTOs
    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class DriveItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; } = string.Empty;
    }

    private class SharingLinkResponse
    {
        [JsonPropertyName("link")]
        public LinkInfo Link { get; set; } = new();
    }

    private class LinkInfo
    {
        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; } = string.Empty;
    }

    private class DriveInfo
    {
        [JsonPropertyName("driveType")]
        public string DriveType { get; set; } = string.Empty;

        [JsonPropertyName("owner")]
        public OwnerInfo Owner { get; set; } = new();

        [JsonPropertyName("quota")]
        public QuotaInfo Quota { get; set; } = new();
    }

    private class OwnerInfo
    {
        [JsonPropertyName("user")]
        public UserInfo User { get; set; } = new();
    }

    private class UserInfo
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;
    }

    private class QuotaInfo
    {
        [JsonPropertyName("total")]
        public long? Total { get; set; }

        [JsonPropertyName("remaining")]
        public long? Remaining { get; set; }
    }
}
