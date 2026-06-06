using MassLab.Common.Caching.Models;

namespace MassLab.Common.Caching.Abstractions;

/// <summary>
/// Extended cache service with Redis-specific operations (Hash, List, Set, SortedSet, Binary).
/// </summary>
public interface IAdvancedCacheService : ICacheService
{
    Task SetBinaryAsync(string key, byte[] data, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    Task<byte[]?> GetBinaryAsync(string key, CancellationToken cancellationToken = default);

    Task HashSetAsync<T>(string key, string field, T value, CancellationToken cancellationToken = default);
    Task<T?> HashGetAsync<T>(string key, string field, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default);

    Task<long> ListPushAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task<T?> ListPopAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<T[]> ListRangeAsync<T>(string key, long start = 0, long stop = -1, CancellationToken cancellationToken = default);
    Task<long> ListLengthAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> SetAddAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task<bool> SetRemoveAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task<T[]> SetMembersAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<bool> SetContainsAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    Task<bool> SortedSetAddAsync<T>(string key, T value, double score, CancellationToken cancellationToken = default);
    Task<bool> SortedSetRemoveAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task<T[]> SortedSetRangeAsync<T>(string key, long start = 0, long stop = -1, SortOrder order = SortOrder.Ascending, CancellationToken cancellationToken = default);
    Task<double?> SortedSetScoreAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}
