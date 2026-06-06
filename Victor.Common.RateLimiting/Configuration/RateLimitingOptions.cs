namespace Victor.Common.RateLimiting.Configuration;

/// <summary>
/// Options for the Victor rate-limiting helper.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>Configuration section name (<c>RateLimiting</c>).</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Permits per window (default 100).</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>Window length in seconds (default 60).</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Queue limit for waiting requests (default 0 — reject immediately).</summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>HTTP status code returned when limit is exceeded (default 429).</summary>
    public int RejectionStatusCode { get; set; } = 429;

    /// <summary>If <c>true</c>, partitions by user id when authenticated, else by IP.</summary>
    public bool UseUserPartitioning { get; set; } = true;

    /// <summary>Default limiter algorithm used when a policy does not specify one.</summary>
    public RateLimiterKind Limiter { get; set; } = RateLimiterKind.FixedWindow;

    /// <summary>Segments per window for sliding-window policies.</summary>
    public int SegmentsPerWindow { get; set; } = 4;

    /// <summary>Token replenishment period in seconds for token-bucket policies.</summary>
    public int ReplenishmentSeconds { get; set; } = 10;

    /// <summary>Tokens added each replenishment period for token-bucket policies.</summary>
    public int TokensPerPeriod { get; set; } = 10;

    /// <summary>Optional named endpoint policies.</summary>
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Supported rate limiter algorithms.</summary>
public enum RateLimiterKind
{
    /// <summary>Fixed-window limiter.</summary>
    FixedWindow,
    /// <summary>Sliding-window limiter.</summary>
    SlidingWindow,
    /// <summary>Token-bucket limiter.</summary>
    TokenBucket
}

/// <summary>Per-policy rate limit options.</summary>
public class RateLimitPolicyOptions
{
    /// <summary>Limiter algorithm.</summary>
    public RateLimiterKind? Limiter { get; set; }

    /// <summary>Permit/token limit.</summary>
    public int? PermitLimit { get; set; }

    /// <summary>Window length in seconds.</summary>
    public int? WindowSeconds { get; set; }

    /// <summary>Queue limit.</summary>
    public int? QueueLimit { get; set; }

    /// <summary>Segments per window for sliding-window policies.</summary>
    public int? SegmentsPerWindow { get; set; }

    /// <summary>Token replenishment period in seconds.</summary>
    public int? ReplenishmentSeconds { get; set; }

    /// <summary>Tokens added per replenishment period.</summary>
    public int? TokensPerPeriod { get; set; }

    /// <summary>Partition strategy: <c>ip</c>, <c>user</c>, or <c>endpoint</c>.</summary>
    public string PartitionBy { get; set; } = "ip";
}
