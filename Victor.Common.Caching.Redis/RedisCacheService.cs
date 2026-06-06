using System.Linq;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Victor.Common.Caching.Abstractions;
using Victor.Common.Caching.Models;
using Victor.Common.Caching.Redis.Configuration;
using Victor.Common.Caching.Redis.Serialization;

namespace Victor.Common.Caching.Redis;

/// <summary>
/// Redis cache implementation providing distributed caching and locking capabilities.
/// </summary>
public class RedisCacheService : IAdvancedCacheService, IDistributedLock
{
    private readonly IDatabase _database;
    private readonly RedisCacheOptions _options;
    private readonly ICacheSerializer _serializer;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisCacheOptions> options, ICacheSerializer? serializer = null)
    {
        if (connectionMultiplexer == null)
            throw new ArgumentNullException(nameof(connectionMultiplexer));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _database = connectionMultiplexer.GetDatabase();
        _options = options.Value;
        _serializer = serializer ?? new JsonCacheSerializer();
    }

    /// <summary>
    /// Gets a scoped key with the instance name prefix.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The scoped key in format "InstanceName:key".</returns>
    private string GetScopedKey(string key)
    {
        if (string.IsNullOrWhiteSpace(_options.InstanceName))
            return key;

        return $"{_options.InstanceName}{_options.KeySeparator}{key}";
    }

    /// <summary>
    /// Gets a global key without the instance name prefix.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The key as-is.</returns>
    private string GetGlobalKey(string key)
    {
        return key;
    }

    /// <summary>
    /// Calculates the expiration TimeSpan from CacheEntryOptions.
    /// </summary>
    /// <param name="options">The cache entry options.</param>
    /// <returns>The expiration TimeSpan, or null if no expiration is set.</returns>
    private TimeSpan? GetExpiry(CacheEntryOptions? options)
    {
        if (options == null)
            return _options.DefaultExpiration;

        if (options.AbsoluteExpirationRelativeToNow.HasValue)
            return options.AbsoluteExpirationRelativeToNow.Value;

        if (options.AbsoluteExpiration.HasValue)
        {
            var timeUntilExpiration = options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;
            return timeUntilExpiration > TimeSpan.Zero ? timeUntilExpiration : TimeSpan.Zero;
        }

        if (options.SlidingExpiration.HasValue)
            return options.SlidingExpiration.Value;

        return _options.DefaultExpiration;
    }

    // ICacheService implementation
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var value = await _database.StringGetAsync(scopedKey);
            
            if (!value.HasValue)
                return default;

            return _serializer.Deserialize<T>(value!, scopedKey);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get value for key '{key}' from Redis.", ex);
        }
    }

    public async Task<T?> GetGlobalAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var globalKey = GetGlobalKey(key);
            var value = await _database.StringGetAsync(globalKey);
            
            if (!value.HasValue)
                return default;

            return _serializer.Deserialize<T>(value!, globalKey);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get global value for key '{key}' from Redis.", ex);
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var json = _serializer.Serialize(value);
            var expiry = GetExpiry(options);

            await _database.StringSetAsync(scopedKey, json, expiry);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to set value for key '{key}' in Redis.", ex);
        }
    }

    public async Task SetGlobalAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var globalKey = GetGlobalKey(key);
            var json = _serializer.Serialize(value);
            var expiry = GetExpiry(options);

            await _database.StringSetAsync(globalKey, json, expiry);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to set global value for key '{key}' in Redis.", ex);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            await _database.KeyDeleteAsync(scopedKey);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to remove key '{key}' from Redis.", ex);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            return await _database.KeyExistsAsync(scopedKey);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to check existence of key '{key}' in Redis.", ex);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null)
            return existing;

        var value = await factory(cancellationToken);
        await SetAsync(key, value, options, cancellationToken);
        return value;
    }

    public async Task SetBinaryAsync(string key, byte[] data, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            var scopedKey = GetScopedKey(key);
            var expiry = GetExpiry(options);

            await _database.StringSetAsync(scopedKey, data, expiry);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to set binary data for key '{key}' in Redis.", ex);
        }
    }

    public async Task<byte[]?> GetBinaryAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var value = await _database.StringGetAsync(scopedKey);
            
            if (!value.HasValue)
                return null;

            return (byte[]?)value;
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get binary data for key '{key}' from Redis.", ex);
        }
    }

    public async Task HashSetAsync<T>(string key, string field, T value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("Field cannot be null or whitespace.", nameof(field));

        try
        {
            var scopedKey = GetScopedKey(key);
            var serialized = _serializer.Serialize(value);
            await _database.HashSetAsync(scopedKey, field, serialized);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to set hash field '{field}' for key '{key}' in Redis.", ex);
        }
    }

    public async Task<T?> HashGetAsync<T>(string key, string field, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("Field cannot be null or whitespace.", nameof(field));

        try
        {
            var scopedKey = GetScopedKey(key);
            var value = await _database.HashGetAsync(scopedKey, field);
            
            if (!value.HasValue)
                return default;

            return _serializer.Deserialize<T>(value!, scopedKey);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get hash field '{field}' for key '{key}' from Redis.", ex);
        }
    }

    public async Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var entries = await _database.HashGetAllAsync(scopedKey);
            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => e.Value.ToString());
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get all hash fields for key '{key}' from Redis.", ex);
        }
    }

    public async Task<bool> HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("Field cannot be null or whitespace.", nameof(field));

        try
        {
            var scopedKey = GetScopedKey(key);
            return await _database.HashDeleteAsync(scopedKey, field);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to delete hash field '{field}' for key '{key}' from Redis.", ex);
        }
    }

    public async Task<long> ListPushAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var serialized = _serializer.Serialize(value);
            return await _database.ListLeftPushAsync(scopedKey, serialized);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to push value to list for key '{key}' in Redis.", ex);
        }
    }

    public async Task<T?> ListPopAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var value = await _database.ListLeftPopAsync(scopedKey);
            
            if (!value.HasValue)
                return default;

            return _serializer.Deserialize<T>(value!, scopedKey);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to pop value from list for key '{key}' in Redis.", ex);
        }
    }

    public async Task<T[]> ListRangeAsync<T>(string key, long start = 0, long stop = -1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var values = await _database.ListRangeAsync(scopedKey, start, stop);
            
            var result = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = _serializer.Deserialize<T>(values[i]!, scopedKey)!;
            }
            
            return result;
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get range from list for key '{key}' in Redis.", ex);
        }
    }

    public async Task<long> ListLengthAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            return await _database.ListLengthAsync(scopedKey);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get length of list for key '{key}' in Redis.", ex);
        }
    }

    public async Task<bool> SetAddAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var serialized = _serializer.Serialize(value);
            return await _database.SetAddAsync(scopedKey, serialized);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to add value to set for key '{key}' in Redis.", ex);
        }
    }

    public async Task<bool> SetRemoveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var serialized = _serializer.Serialize(value);
            return await _database.SetRemoveAsync(scopedKey, serialized);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to remove value from set for key '{key}' in Redis.", ex);
        }
    }

    public async Task<T[]> SetMembersAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var values = await _database.SetMembersAsync(scopedKey);
            
            var result = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = _serializer.Deserialize<T>(values[i]!, scopedKey)!;
            }
            
            return result;
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get members from set for key '{key}' in Redis.", ex);
        }
    }

    public async Task<bool> SetContainsAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var serialized = _serializer.Serialize(value);
            return await _database.SetContainsAsync(scopedKey, serialized);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to check set membership for key '{key}' in Redis.", ex);
        }
    }

    public async Task<bool> SortedSetAddAsync<T>(string key, T value, double score, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var serialized = _serializer.Serialize(value);
            return await _database.SortedSetAddAsync(scopedKey, serialized, score);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to add value to sorted set for key '{key}' in Redis.", ex);
        }
    }

    public async Task<bool> SortedSetRemoveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var serialized = _serializer.Serialize(value);
            return await _database.SortedSetRemoveAsync(scopedKey, serialized);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to remove value from sorted set for key '{key}' in Redis.", ex);
        }
    }

    public async Task<T[]> SortedSetRangeAsync<T>(string key, long start = 0, long stop = -1, SortOrder order = SortOrder.Ascending, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var redisOrder = order == SortOrder.Ascending ? Order.Ascending : Order.Descending;
            var values = await _database.SortedSetRangeByRankAsync(scopedKey, start, stop, redisOrder);
            
            var result = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = _serializer.Deserialize<T>(values[i]!, scopedKey)!;
            }
            
            return result;
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get range from sorted set for key '{key}' in Redis.", ex);
        }
    }

    public async Task<double?> SortedSetScoreAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var scopedKey = GetScopedKey(key);
            var serialized = _serializer.Serialize(value);
            return await _database.SortedSetScoreAsync(scopedKey, serialized);
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to get score from sorted set for key '{key}' in Redis.", ex);
        }
    }

    // IDistributedLock implementation
    public async Task<LockToken?> AcquireLockAsync(string key, TimeSpan timeout, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout cannot be negative.");
        if (expiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expiration), expiration, "Expiration must be greater than zero.");

        var lockKey = GetLockKey(key);
        var token = Guid.NewGuid().ToString("N");
        var endTime = DateTimeOffset.UtcNow.Add(timeout);
        var delayMs = 50;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // SET NX EX: Set if Not eXists with EXpiration
                var acquired = await _database.StringSetAsync(
                    lockKey, 
                    token, 
                    expiration, 
                    When.NotExists);

                if (acquired)
                    return new LockToken(key, token);
            }
            catch (RedisException ex)
            {
                throw new Exceptions.CacheConnectionException($"Failed to acquire lock for key '{key}' in Redis.", ex);
            }

            if (DateTimeOffset.UtcNow >= endTime)
                return null;

            var remainingDelay = endTime - DateTimeOffset.UtcNow;
            if (remainingDelay <= TimeSpan.Zero)
                return null;

            await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(delayMs, remainingDelay.TotalMilliseconds)), cancellationToken);
            delayMs = Math.Min(delayMs * 2, 1000);
        }
    }

    public async Task<bool> ReleaseLockAsync(LockToken token, CancellationToken cancellationToken = default)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        var lockKey = GetLockKey(token.Key);

        try
        {
            // Lua script to atomically check token and delete
            const string script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            var result = await _database.ScriptEvaluateAsync(
                script, 
                new RedisKey[] { lockKey }, 
                new RedisValue[] { token.Token });

            return (int)result == 1;
        }
        catch (RedisException ex)
        {
            throw new Exceptions.CacheConnectionException($"Failed to release lock for key '{token.Key}' in Redis.", ex);
        }
    }

    private static string GetLockKey(string key) => $"lock:{key}";
}
