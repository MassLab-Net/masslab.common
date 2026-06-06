namespace MassLab.Common.Caching.Redis.Configuration;

/// <summary>
/// Configuration options for Redis caching.
/// </summary>
public class RedisCacheOptions
{
    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the instance name prefix for scoped cache keys.
    /// Keys will be formatted as "InstanceName:key" to create a folder-like structure.
    /// Example: "myapp" results in keys like "myapp:user:123"
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key separator used between instance name and key.
    /// Default is ":" to follow Redis convention for hierarchical keys.
    /// </summary>
    public string KeySeparator { get; set; } = ":";

    /// <summary>
    /// Gets or sets the default expiration time for cache entries.
    /// </summary>
    public TimeSpan? DefaultExpiration { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout.
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the operation timeout.
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(1);
}
