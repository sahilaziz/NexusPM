using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Services;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;
using Nexus.Infrastructure.Storage;

namespace Nexus.Infrastructure.Repositories;

public class StorageSettingsRepository : IStorageSettingsRepository
{
    private readonly AppDbContext _context;

    public StorageSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StorageSettings?> GetByIdAsync(int storageId)
    {
        var entity = await _context.StorageSettings.FindAsync(storageId);
        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<StorageSettings?> GetDefaultAsync()
    {
        var entity = await _context.StorageSettings
            .FirstOrDefaultAsync(s => s.IsDefault && s.IsActive);
        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<StorageSettings?> GetByTypeAsync(StorageType type)
    {
        var entity = await _context.StorageSettings
            .FirstOrDefaultAsync(s => s.Type == type && s.IsActive);
        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<IEnumerable<StorageSettings>> GetAllActiveAsync()
    {
        var entities = await _context.StorageSettings
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.StorageName)
            .ToListAsync();

        return entities.Select(MapToDomain);
    }

    public async Task<StorageSettings> CreateAsync(StorageSettings settings)
    {
        // Əgər default seçilibsə, digərlərini default-dan çıxar
        if (settings.IsDefault)
        {
            await UnsetAllDefaultAsync();
        }

        var entity = new StorageSettingsEntity
        {
            StorageName = settings.StorageName,
            Type = settings.Type,
            IsDefault = settings.IsDefault,
            IsActive = settings.IsActive,
            ConfigurationJson = JsonSerializer.Serialize(settings.Configuration),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = settings.CreatedBy
        };

        _context.StorageSettings.Add(entity);
        await _context.SaveChangesAsync();

        settings.StorageId = entity.StorageId;
        return settings;
    }

    public async Task UpdateAsync(StorageSettings settings)
    {
        var entity = await _context.StorageSettings.FindAsync(settings.StorageId);
        if (entity == null)
            throw new InvalidOperationException($"Storage settings not found: {settings.StorageId}");

        // Əgər default seçilibsə, digərlərini default-dan çıxar
        if (settings.IsDefault && !entity.IsDefault)
        {
            await UnsetAllDefaultAsync();
        }

        entity.StorageName = settings.StorageName;
        entity.Type = settings.Type;
        entity.IsDefault = settings.IsDefault;
        entity.IsActive = settings.IsActive;
        entity.ConfigurationJson = JsonSerializer.Serialize(settings.Configuration);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int storageId)
    {
        var entity = await _context.StorageSettings.FindAsync(storageId);
        if (entity != null)
        {
            entity.IsActive = false;
            entity.IsDefault = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetDefaultAsync(int storageId)
    {
        await UnsetAllDefaultAsync();

        var entity = await _context.StorageSettings.FindAsync(storageId);
        if (entity != null)
        {
            entity.IsDefault = true;
            await _context.SaveChangesAsync();
        }
    }

    private async Task UnsetAllDefaultAsync()
    {
        var defaults = await _context.StorageSettings
            .Where(s => s.IsDefault)
            .ToListAsync();

        foreach (var d in defaults)
        {
            d.IsDefault = false;
        }

        await _context.SaveChangesAsync();
    }

    private static StorageSettings MapToDomain(StorageSettingsEntity entity)
    {
        return new StorageSettings
        {
            StorageId = entity.StorageId,
            StorageName = entity.StorageName,
            Type = entity.Type,
            IsDefault = entity.IsDefault,
            IsActive = entity.IsActive,
            Configuration = JsonSerializer.Deserialize<StorageConfiguration>(entity.ConfigurationJson) 
                ?? new StorageConfiguration(),
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
    }
}

/// <summary>
/// Database entity for storage settings
/// </summary>
public class StorageSettingsEntity
{
    public int StorageId { get; set; }
    public string StorageName { get; set; } = string.Empty;
    public StorageType Type { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string ConfigurationJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
