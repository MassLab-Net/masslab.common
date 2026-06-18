namespace MassLab.Common.RateLimiting.Configuration;

/// <summary>
/// Configuration for IP-based rate limit partitioning.
/// </summary>
public class IpPartitionOptions
{
    /// <summary>Default policy applied to all IPs without specific config.</summary>
    public ClientRateLimitPolicy? DefaultPolicy { get; set; }
    
    /// <summary>Per-IP rate limit policies. Key = IP address or wildcard pattern (e.g., "10.0.0.*").</summary>
    public Dictionary<string, ClientRateLimitPolicy> Policies { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
