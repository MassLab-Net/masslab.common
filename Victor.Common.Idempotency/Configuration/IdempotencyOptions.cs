namespace Victor.Common.Idempotency.Configuration;

/// <summary>Options for idempotent HTTP write handling.</summary>
public class IdempotencyOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Idempotency";

    /// <summary>Header containing the idempotency key.</summary>
    public string HeaderName { get; set; } = "Idempotency-Key";

    /// <summary>Cache key prefix.</summary>
    public string CacheKeyPrefix { get; set; } = "idempotency";

    /// <summary>How long successful responses are cached.</summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Only these methods are deduplicated by the middleware.</summary>
    public string[] Methods { get; set; } = ["POST", "PUT", "PATCH"];

    /// <summary>When true, missing keys on configured write methods return 400.</summary>
    public bool RequireHeader { get; set; }
}
