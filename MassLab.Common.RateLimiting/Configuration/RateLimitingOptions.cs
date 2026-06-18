
namespace MassLab.Common.RateLimiting.Configuration;

/// <summary>
/// Options for the MassLab rate-limiting helper.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Permits per window (default 100).</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>Window length in seconds (default 60).</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Queue limit for waiting requests (default 0).</summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>HTTP status code when limit exceeded (default 429).</summary>
    public int RejectionStatusCode { get; set; } = 429;

    /// <summary>Default limiter algorithm.</summary>
    public RateLimiterKind Limiter { get; set; } = RateLimiterKind.FixedWindow;

    /// <summary>Segments per window for sliding-window.</summary>
    public int SegmentsPerWindow { get; set; } = 4;

    /// <summary>Token replenishment period in seconds.</summary>
    public int ReplenishmentSeconds { get; set; } = 10;

    /// <summary>Tokens added per replenishment period.</summary>
    public int TokensPerPeriod { get; set; } = 10;

    /// <summary>If true, each endpoint has its own rate limit bucket.</summary>
    public bool PerEndpoint { get; set; } = false;

    /// <summary>Partition strategy: "user" or "ip".</summary>
    public string PartitionBy { get; set; } = "user";

    /// <summary>User-based partition configuration.</summary>
    public UserPartitionOptions? UserPartition { get; set; }

    /// <summary>IP-based partition configuration.</summary>
    public IpPartitionOptions? IpPartition { get; set; }

    /// <summary>Named endpoint policies for [EnableRateLimiting] attribute.</summary>
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Backward compatibility
    [Obsolete("Use PartitionBy = 'user' instead.")]
    public bool UseUserPartitioning
    {
        get => string.Equals(PartitionBy, "user", StringComparison.OrdinalIgnoreCase);
        set => PartitionBy = value ? "user" : "ip";
    }
}

/// <summary>Supported rate limiter algorithms.</summary>
public enum RateLimiterKind
{
    FixedWindow,
    SlidingWindow,
    TokenBucket
}

/// <summary>Per-policy rate limit options.</summary>
public class RateLimitPolicyOptions
{
    public RateLimiterKind? Limiter { get; set; }
    public int? PermitLimit { get; set; }
    public int? WindowSeconds { get; set; }
    public int? QueueLimit { get; set; }
    public int? SegmentsPerWindow { get; set; }
    public int? ReplenishmentSeconds { get; set; }
    public int? TokensPerPeriod { get; set; }
    
    /// <summary>If true, each endpoint has its own bucket.</summary>
    public bool? PerEndpoint { get; set; }

    /// <summary>Partition strategy (for named policies only).</summary>
    public string PartitionBy { get; set; } = "ip";
}