using Microsoft.Extensions.Caching.Memory;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Application.Interfaces.Services;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Storage;

/// <summary>
/// Storage factory - düzgün storage servisini yaradır
/// </summary>
public class StorageFactory : IStorageFactory
{
    private readonly IStorageSettingsRepository _settingsRepository;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<int, IStorageService> _storageInstances = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public StorageFactory(
        IStorageSettingsRepository settingsRepository,
        ILoggerFactory loggerFactory,
        IMemoryCache cache)
    {
        _settingsRepository = settingsRepository;
        _loggerFactory = loggerFactory;
        _cache = cache;
    }

    public async Task<IStorageService> GetDefaultStorageAsync()
    {
        var settings = await _settingsRepository.GetDefaultAsync();
        if (settings == null)
        {
            throw new InvalidOperationException("No default storage configured");
        }

        return await GetStorageAsync(settings.StorageId);
    }

    public Task<IStorageService> GetStorageAsync(StorageType type)
    {
        // Type-a görə ilk aktiv storage-i tap
        var settings = _settingsRepository.GetByTypeAsync(type);
        return GetStorageAsync(settings.Result?.StorageId 
            ?? throw new InvalidOperationException($"No storage found for type: {type}"));
    }

    public async Task<IStorageService> GetStorageAsync(int storageId)
    {
        // Cache-dən yoxla
        if (_storageInstances.TryGetValue(storageId, out var cachedStorage))
        {
            return cachedStorage;
        }

        await _semaphore.WaitAsync();
        try
        {
            // Bir daha yoxla (thread-safe)
            if (_storageInstances.TryGetValue(storageId, out cachedStorage))
            {
                return cachedStorage;
            }

            var settings = await _settingsRepository.GetByIdAsync(storageId);
            if (settings == null || !settings.IsActive)
            {
                throw new InvalidOperationException($"Storage not found or inactive: {storageId}");
            }

            IStorageService storage = settings.Type switch
            {
                StorageType.LocalDisk => new LocalDiskStorageService(
                    settings, 
                    _loggerFactory.CreateLogger<LocalDiskStorageService>()),
                
                StorageType.FtpServer => new FtpStorageService(
                    settings, 
                    _loggerFactory.CreateLogger<FtpStorageService>()),
                
                StorageType.OneDrive => new OneDriveStorageService(
                    settings, 
                    _loggerFactory.CreateLogger<OneDriveStorageService>(),
                    _cache),
                
                StorageType.GoogleDrive => throw new NotImplementedException("Google Drive not yet implemented"),
                
                StorageType.NetworkShare => throw new NotImplementedException("Network share not yet implemented"),
                
                _ => throw new NotSupportedException($"Storage type not supported: {settings.Type}")
            };

            _storageInstances[storageId] = storage;
            return storage;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<IStorageService>> GetAllActiveStoragesAsync()
    {
        var settings = await _settingsRepository.GetAllActiveAsync();
        var storages = new List<IStorageService>();

        foreach (var setting in settings)
        {
            try
            {
                var storage = await GetStorageAsync(setting.StorageId);
                storages.Add(storage);
            }
            catch (Exception ex)
            {
                // Log but continue
                var logger = _loggerFactory.CreateLogger<StorageFactory>();
                logger.LogError(ex, "Failed to create storage service for ID: {StorageId}", setting.StorageId);
            }
        }

        return storages;
    }

    /// <summary>
    /// Storage instansını təmizlə (konfiqurasiya dəyişəndə)
    /// </summary>
    public void InvalidateStorage(int storageId)
    {
        _storageInstances.Remove(storageId);
    }
}

/// <summary>
/// Storage settings repository interface
/// </summary>
public interface IStorageSettingsRepository
{
    Task<StorageSettings?> GetByIdAsync(int storageId);
    Task<StorageSettings?> GetDefaultAsync();
    Task<StorageSettings?> GetByTypeAsync(StorageType type);
    Task<IEnumerable<StorageSettings>> GetAllActiveAsync();
    Task<StorageSettings> CreateAsync(StorageSettings settings);
    Task UpdateAsync(StorageSettings settings);
    Task DeleteAsync(int storageId);
    Task SetDefaultAsync(int storageId);
}
