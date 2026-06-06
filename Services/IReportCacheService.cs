using System;
using System.Threading.Tasks;

namespace OmniBizAI.Services;

public interface IReportCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> acquire, TimeSpan? expiration = null);
    Task InvalidateTenantCacheAsync();
}
