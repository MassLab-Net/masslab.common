namespace MassLab.Common.RateLimiting.Configuration;

/// <summary>
/// Rate limit policy for a specific client (user or IP).
/// </summary>
public class ClientRateLimitPolicy
{
    /// <summary>Default limit applied when no endpoint override matches.</summary>
    public RateLimitPolicyOptions? DefaultLimit { get; set; }

    /// <summary>Endpoint-specific overrides. Supports wildcard patterns (e.g., "/api/ai/*").</summary>
    public Dictionary<string, RateLimitPolicyOptions> EndpointOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
