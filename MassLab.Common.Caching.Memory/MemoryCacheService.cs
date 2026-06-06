using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MassLab.Common.Caching.Abstractions;
using MassLab.Common.Caching.Memory.Configuration;
using MassLab.Common.Caching.Models;

namespace MassLab.Common.Caching.Memory;

/// <summary>
/// In-memory cache implementation using Microsoft.Extensions.Caching.Memory.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly Configuration.MassLabMemoryCacheOptions _options;

    public MemoryCacheService(IMemoryCache cache, IOptions<Configuration.MassLabMemoryCacheOptions> options)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        var value = _cache.Get<T>(key);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        var entryOptions = new MemoryCacheEntryOptions();

        if (options?.AbsoluteExpiration.HasValue == true)
        {
            entryOptions.AbsoluteExpiration = options.AbsoluteExpiration;
        }
        else if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
        {
            entryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
        }

        if (options?.SlidingExpiration.HasValue == true)
        {
            entryOptions.SlidingExpiration = options.SlidingExpiration;
        }

        if (entryOptions.AbsoluteExpiration is null && entryOptions.AbsoluteExpirationRelativeToNow is null
            && entryOptions.SlidingExpiration is null && _options.DefaultExpiration.HasValue)
        {
            entryOptions.AbsoluteExpirationRelativeToNow = _options.DefaultExpiration;
        }

        // Microsoft.Extensions.Caching.Memory requires every entry to specify a Size when SizeLimit is configured.
        // Default to 1 (entry-count semantics) so callers do not have to set it explicitly.
        if (_options.SizeLimit.HasValue && entryOptions.Size is null)
        {
            entryOptions.Size = 1;
        }

        _cache.Set(key, value, entryOptions);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        var exists = _cache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    // Global key operations (memory cache doesn't distinguish between scoped and global)
    public Task<T?> GetGlobalAsync<T>(string key, CancellationToken cancellationToken = default)
        => GetAsync<T>(key, cancellationToken);

    public Task SetGlobalAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => SetAsync(key, value, options, cancellationToken);

    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null)
            return existing;

        var value = await factory(cancellationToken);
        await SetAsync(key, value, options, cancellationToken);
        return value;
    }
}
