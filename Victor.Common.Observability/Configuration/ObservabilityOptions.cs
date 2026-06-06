namespace Victor.Common.Observability.Configuration;

/// <summary>
/// Options for the Victor observability registration helper.
/// </summary>
public class ObservabilityOptions
{
    /// <summary>Configuration section name (<c>Observability</c>).</summary>
    public const string SectionName = "Observability";

    /// <summary>Service name reported to OTel (e.g. <c>ProductApi</c>).</summary>
    public string ServiceName { get; set; } = "victor-service";

    /// <summary>Service version (defaults to assembly version).</summary>
    public string? ServiceVersion { get; set; }

    /// <summary>Deployment environment (e.g. <c>production</c>).</summary>
    public string? Environment { get; set; }

    /// <summary>OTLP endpoint (defaults to <c>http://localhost:4317</c>).</summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>Enable Prometheus scrape endpoint (default <c>true</c>).</summary>
    public bool EnablePrometheus { get; set; } = true;

    /// <summary>Path of the Prometheus scrape endpoint (default <c>/metrics</c>).</summary>
    public string PrometheusEndpoint { get; set; } = "/metrics";

    /// <summary>Enable OTel traces (default <c>true</c>).</summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>Enable OTel metrics (default <c>true</c>).</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Enable OTel logging (default <c>false</c>; use Serilog OTel sink instead).</summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>Enable EFCore instrumentation (default <c>true</c>).</summary>
    public bool EnableEntityFrameworkCore { get; set; } = true;

    /// <summary>Enable gRPC client instrumentation (default <c>true</c>).</summary>
    public bool EnableGrpcClient { get; set; } = true;

    /// <summary>Enable Redis instrumentation (default <c>true</c>).</summary>
    public bool EnableRedis { get; set; } = true;
}
