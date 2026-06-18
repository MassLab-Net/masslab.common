namespace MassLab.Common.RateLimiting.Configuration;

/// <summary>
/// Configuration for user-based rate limit partitioning.
/// </summary>
public class UserPartitionOptions
{
    /// <summary>Claim name to extract user ID (default: "sub").</summary>
    public string ClaimName { get; set; } = "sub";

    /// <summary>Fallback claim name if primary not found.</summary>
    public string? FallbackClaimName { get; set; }

    /// <summary>Per-user rate limit policies. Key = user ID.</summary>
    public Dictionary<string, ClientRateLimitPolicy> Policies { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
