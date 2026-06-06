namespace Victor.Common.Api.Configuration;

/// <summary>
/// Health check configuration options.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether health checks are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout in seconds for health checks.
    /// </summary>
    public int Timeout { get; set; } = 5;

    /// <summary>
    /// Gets or sets the health check endpoints.
    /// </summary>
    public HealthCheckEndpoints Endpoints { get; set; } = new();
}

/// <summary>
/// Health check endpoint configuration.
/// </summary>
public class HealthCheckEndpoints
{
    /// <summary>
    /// Gets or sets the main health check endpoint.
    /// </summary>
    public string Health { get; set; } = "/health";

    /// <summary>
    /// Gets or sets the readiness check endpoint.
    /// </summary>
    public string Ready { get; set; } = "/health/ready";

    /// <summary>
    /// Gets or sets the liveness check endpoint.
    /// </summary>
    public string Live { get; set; } = "/health/live";
}
