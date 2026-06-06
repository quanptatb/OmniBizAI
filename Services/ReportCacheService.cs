using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
namespace OmniBizAI.Services;

public class ReportCacheService : IReportCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ReportCacheService> _logger;
    private readonly TimeSpan _l1Expiration = TimeSpan.FromMinutes(2); // Local RAM Cache
    private readonly TimeSpan _l2Expiration = TimeSpan.FromMinutes(10); // Redis Cache
    private static bool _isRedisOffline = false;
    private static DateTime _lastRedisCheck = DateTime.MinValue;

    public ReportCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ITenantContext tenantContext,
        ILogger<ReportCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    private async Task<bool> CheckRedisStatusWithFallbackAsync()
    {
        if (!_isRedisOffline) return true;

        // If Redis was marked offline, try to reconnect every 60 seconds
        if ((DateTime.UtcNow - _lastRedisCheck).TotalSeconds > 60)
        {
            try
            {
                await _distributedCache.GetAsync("PingRedis");
                _isRedisOffline = false;
                _logger.LogInformation("Redis has recovered and is now online.");
            }
            catch
            {
                _lastRedisCheck = DateTime.UtcNow;
            }
        }

        return !_isRedisOffline;
    }

    private async Task<string> GetTenantVersionAsync()
    {
        var tenantId = _tenantContext.TenantId;
        var versionKey = $"TenantVersion:{tenantId}";

        // 1. Try to read from L1 MemoryCache first (super fast, no network)
        if (_memoryCache.TryGetValue(versionKey, out string? version) && !string.IsNullOrEmpty(version))
        {
            return version;
        }

        version = null;

        // 2. Try to read from L2 Redis if online
        if (await CheckRedisStatusWithFallbackAsync())
        {
            try
            {
                var versionBytes = await _distributedCache.GetAsync(versionKey);
                if (versionBytes != null && versionBytes.Length > 0)
                {
                    version = System.Text.Encoding.UTF8.GetString(versionBytes);
                }
            }
            catch (Exception ex)
            {
                _isRedisOffline = true;
                _lastRedisCheck = DateTime.UtcNow;
                _logger.LogWarning(ex, "Redis is offline. Falling back to MemoryCache for Tenant Version.");
            }
        }

        // 3. Generate a new version if not found in L1 and L2
        if (string.IsNullOrEmpty(version))
        {
            version = Guid.NewGuid().ToString();
            
            if (!_isRedisOffline)
            {
                try
                {
                    await _distributedCache.SetAsync(versionKey, System.Text.Encoding.UTF8.GetBytes(version), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                    });
                }
                catch (Exception ex)
                {
                    _isRedisOffline = true;
                    _lastRedisCheck = DateTime.UtcNow;
                    _logger.LogWarning(ex, "Failed to save new Tenant Version to Redis.");
                }
            }
        }

        // Sync L1 MemoryCache
        _memoryCache.Set(versionKey, version, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
        });

        return version;
    }

    private async Task<string> BuildFullKeyAsync(string key)
    {
        var tenantId = _tenantContext.TenantId;
        var version = await GetTenantVersionAsync();
        return $"Report:{tenantId}:{version}:{key}";
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var fullKey = await BuildFullKeyAsync(key);

        // L1 Cache: MemoryCache Check
        if (_memoryCache.TryGetValue(fullKey, out T? localValue))
        {
            return localValue;
        }

        // L2 Cache: Redis Check
        if (await CheckRedisStatusWithFallbackAsync())
        {
            try
            {
                var dataBytes = await _distributedCache.GetAsync(fullKey);
                if (dataBytes != null && dataBytes.Length > 0)
                {
                    var value = JsonSerializer.Deserialize<T>(dataBytes);
                    if (value != null)
                    {
                        // Sync L1 Cache for subsequent reads
                        _memoryCache.Set(fullKey, value, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _l1Expiration
                        });
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                _isRedisOffline = true;
                _lastRedisCheck = DateTime.UtcNow;
                _logger.LogWarning(ex, "Redis exception while getting key {Key}. Falling back to MemoryCache.", fullKey);
            }
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (value == null) return;
        var fullKey = await BuildFullKeyAsync(key);
        var timeToLive = expiration ?? _l2Expiration;

        // Write to L1 MemoryCache (always succeeds)
        _memoryCache.Set(fullKey, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _l1Expiration
        });

        // Write to L2 Redis Cache
        if (await CheckRedisStatusWithFallbackAsync())
        {
            try
            {
                var dataBytes = JsonSerializer.SerializeToUtf8Bytes(value);
                await _distributedCache.SetAsync(fullKey, dataBytes, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = timeToLive
                });
            }
            catch (Exception ex)
            {
                _isRedisOffline = true;
                _lastRedisCheck = DateTime.UtcNow;
                _logger.LogWarning(ex, "Redis exception while setting key {Key}.", fullKey);
            }
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> acquire, TimeSpan? expiration = null)
    {
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;

        var result = await acquire();
        await SetAsync(key, result, expiration);
        return result;
    }

    public async Task InvalidateTenantCacheAsync()
    {
        var tenantId = _tenantContext.TenantId;
        var versionKey = $"TenantVersion:{tenantId}";
        var newVersion = Guid.NewGuid().ToString();

        // 1. Invalidate L1 MemoryCache
        _memoryCache.Set(versionKey, newVersion, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
        });

        // 2. Invalidate L2 Redis Cache
        if (await CheckRedisStatusWithFallbackAsync())
        {
            try
            {
                await _distributedCache.SetAsync(versionKey, System.Text.Encoding.UTF8.GetBytes(newVersion), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                });
            }
            catch (Exception ex)
            {
                _isRedisOffline = true;
                _lastRedisCheck = DateTime.UtcNow;
                _logger.LogWarning(ex, "Failed to invalidate Redis Tenant Version.");
            }
        }
    }
}
