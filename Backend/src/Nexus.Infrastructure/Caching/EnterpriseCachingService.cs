using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Nexus.Infrastructure.Caching;

/// <summary>
/// Enterprise multi-layer caching for 5000+ users
/// L1: In-Memory (per server)
/// L2: Redis (distributed)
/// L3: CDN (static content)
/// </summary>
public interface IEnterpriseCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, CachePolicy policy, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, CachePolicy policy, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> AcquireLockAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);
    Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default);
}

public class EnterpriseCacheService : IEnterpriseCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<EnterpriseCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly TimeSpan DefaultLocalCacheTime = TimeSpan.FromMinutes(5);

    public EnterpriseCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<EnterpriseCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // L1: Try memory cache first (fastest)
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("L1 Cache HIT: {Key}", key);
            return cachedValue;
        }

        // L2: Try distributed cache
        var distributedData = await _distributedCache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(distributedData))
        {
            _logger.LogDebug("L2 Cache HIT: {Key}", key);
            var value = JsonSerializer.Deserialize<T>(distributedData, _jsonOptions);
            
            // Populate L1 cache
            _memoryCache.Set(key, value, DefaultLocalCacheTime);
            
            return value;
        }

        _logger.LogDebug("Cache MISS: {Key}", key);
        return default;
    }

    public async Task SetAsync<T>(string key, T value, CachePolicy policy, CancellationToken cancellationToken = default)
    {
        var serialized = JsonSerializer.Serialize(value, _jsonOptions);

        // L1: Set in-memory cache (short duration)
        var localExpiration = policy.LocalExpiration ?? DefaultLocalCacheTime;
        _memoryCache.Set(key, value, localExpiration);

        // L2: Set distributed cache (longer duration)
        var distributedExpiration = policy.DistributedExpiration ?? TimeSpan.FromHours(1);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = distributedExpiration
        };
        
        await _distributedCache.SetStringAsync(key, serialized, options, cancellationToken);
        
        _logger.LogDebug("Cache SET: {Key}, L1: {LocalExpiry}, L2: {DistributedExpiry}", 
            key, localExpiration, distributedExpiration);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        await _distributedCache.RemoveAsync(key, cancellationToken);
        _logger.LogDebug("Cache REMOVE: {Key}", key);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, CachePolicy policy, CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Cache-aside pattern with lock to prevent cache stampede
        var lockKey = $"lock:{key}";
        var acquired = await AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10), cancellationToken);
        
        if (!acquired)
        {
            // Another thread is populating cache, wait and retry
            await Task.Delay(100, cancellationToken);
            return await GetOrCreateAsync(key, factory, policy, cancellationToken);
        }

        try
        {
            // Double-check after acquiring lock
            cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            // Generate value
            var value = await factory();
            
            // Store in cache
            await SetAsync(key, value, policy, cancellationToken);
            
            return value;
        }
        finally
        {
            await ReleaseLockAsync(lockKey, cancellationToken);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Redis pattern deletion requires SCAN command
        // This is a simplified implementation
        _logger.LogInformation("Removing cache entries by pattern: {Pattern}", pattern);
        
        // Clear all L1 cache for simplicity
        // In production, use Redis SCAN + Lua script
        if (_memoryCache is MemoryCache memCache)
        {
            memCache.Compact(1.0); // Clear all
        }
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        var lockValue = Guid.NewGuid().ToString();
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        try
        {
            var existing = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(existing))
            {
                return false; // Lock already held
            }

            await _distributedCache.SetStringAsync(key, lockValue, options, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default)
    {
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }
}

/// <summary>
/// Cache policy configuration
/// </summary>
public class CachePolicy
{
    public TimeSpan? LocalExpiration { get; set; }
    public TimeSpan? DistributedExpiration { get; set; }

    // Predefined policies
    public static CachePolicy ShortLived => new()
    {
        LocalExpiration = TimeSpan.FromMinutes(1),
        DistributedExpiration = TimeSpan.FromMinutes(5)
    };

    public static CachePolicy Default => new()
    {
        LocalExpiration = TimeSpan.FromMinutes(5),
        DistributedExpiration = TimeSpan.FromHours(1)
    };

    public static CachePolicy LongLived => new()
    {
        LocalExpiration = TimeSpan.FromMinutes(30),
        DistributedExpiration = TimeSpan.FromHours(24)
    };

    public static CachePolicy DocumentCache => new()
    {
        LocalExpiration = TimeSpan.FromMinutes(10),
        DistributedExpiration = TimeSpan.FromHours(2)
    };

    public static CachePolicy UserCache => new()
    {
        LocalExpiration = TimeSpan.FromMinutes(15),
        DistributedExpiration = TimeSpan.FromHours(8)
    };

    public static CachePolicy SearchResults => new()
    {
        LocalExpiration = TimeSpan.FromMinutes(5),
        DistributedExpiration = TimeSpan.FromMinutes(30)
    };
}

/// <summary>
/// Cache key helpers
/// </summary>
public static class CacheKeys
{
    public static string UserProfile(string userId) => $"user:{userId}:profile";
    public static string UserDocuments(string userId) => $"user:{userId}:documents";
    public static string Document(long documentId) => $"doc:{documentId}";
    public static string SearchResults(string query) => $"search:{query.GetHashCode()}";
    public static string Organization(string orgCode) => $"org:{orgCode}";
    public static string TreeStructure(string orgCode) => $"tree:{orgCode}";
    public static string SyncQueue(string userId) => $"sync:{userId}:queue";
}
