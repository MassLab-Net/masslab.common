using MassLab.Common.Caching.Models;

namespace MassLab.Common.Caching.Abstractions;

/// <summary>
/// Defines the contract for cache operations supporting both in-memory and distributed caching.
/// </summary>
public interface ICacheService
{
    // ===== String/Object Operations =====
    
    /// <summary>
    /// Retrieves a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value, or null if not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a value from the cache using a global key (bypasses instance name prefix).
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The global cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value, or null if not found.</returns>
    /// <remarks>Use this for keys that should be shared across all service instances.</remarks>
    Task<T?> GetGlobalAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="options">Optional cache entry options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value in the cache using a global key (bypasses instance name prefix).
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The global cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="options">Optional cache entry options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>Use this for keys that should be shared across all service instances.</remarks>
    Task SetGlobalAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the key exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    // ===== M2: Get-or-set pattern =====

    /// <summary>
    /// Gets a value from cache, or creates and caches it using the factory if not found.
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);
}
