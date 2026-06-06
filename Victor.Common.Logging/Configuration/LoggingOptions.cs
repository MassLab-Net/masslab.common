namespace Victor.Common.Logging.Configuration;

/// <summary>
/// Configuration options for logging.
/// </summary>
public class LoggingOptions
{
    /// <summary>Gets or sets the minimum log level.</summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>Gets or sets per-namespace minimum log level overrides.</summary>
    public Dictionary<string, string> MinimumLevelOverrides { get; set; } = new();

    /// <summary>Whether console logging is enabled.</summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>Whether file logging is enabled.</summary>
    public bool EnableFile { get; set; } = false;

    /// <summary>The file path for file logging (rolling daily).</summary>
    public string? FilePath { get; set; }

    /// <summary>Whether Seq logging is enabled.</summary>
    public bool EnableSeq { get; set; } = false;

    /// <summary>The Seq server URL.</summary>
    public string? SeqUrl { get; set; }

    /// <summary>Whether Application Insights logging is enabled.</summary>
    public bool EnableApplicationInsights { get; set; } = false;

    /// <summary>The Application Insights instrumentation key.</summary>
    public string? ApplicationInsightsKey { get; set; }

    // ─── OpenTelemetry sink ──────────────────────────────────────────────

    /// <summary>Whether the OpenTelemetry log sink is enabled (defaults to <c>false</c>).</summary>
    public bool EnableOpenTelemetry { get; set; } = false;

    /// <summary>
    /// OTLP endpoint for log export (defaults to <c>http://localhost:4317</c>).
    /// </summary>
    public string OpenTelemetryEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Optional OTLP protocol (<c>Grpc</c> [default] or <c>HttpProtobuf</c>).
    /// </summary>
    public string OpenTelemetryProtocol { get; set; } = "Grpc";

    /// <summary>
    /// Optional service name attached to OTel logs (defaults to entry-assembly name).
    /// </summary>
    public string? ServiceName { get; set; }
}
