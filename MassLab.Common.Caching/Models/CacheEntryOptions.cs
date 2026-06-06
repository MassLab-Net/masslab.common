namespace MassLab.Common.Caching.Models;

/// <summary>
/// Options for configuring cache entry expiration.
/// </summary>
public class CacheEntryOptions
{
    /// <summary>
    /// Gets or sets the absolute expiration time.
    /// The entry will expire at this specific point in time.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Gets or sets the absolute expiration relative to now.
    /// The entry will expire after this duration from when it's set.
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration.
    /// The entry will expire if not accessed within this duration.
    /// Each access resets the expiration window.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }
}
