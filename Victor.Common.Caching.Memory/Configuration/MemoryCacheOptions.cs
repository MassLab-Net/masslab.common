namespace Victor.Common.Caching.Memory.Configuration;

/// <summary>
/// Configuration options for in-memory caching.
/// </summary>
public class VictorMemoryCacheOptions
{
    /// <summary>
    /// Gets or sets the maximum size of the cache.
    /// When the cache reaches this size, compaction will occur.
    /// </summary>
    public long? SizeLimit { get; set; }

    /// <summary>
    /// Gets or sets the percentage of entries to remove during compaction.
    /// Value should be between 0.0 and 1.0 (e.g., 0.2 for 20%).
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.2;

    /// <summary>
    /// Gets or sets the default expiration time for cache entries.
    /// </summary>
    public TimeSpan? DefaultExpiration { get; set; }
}
