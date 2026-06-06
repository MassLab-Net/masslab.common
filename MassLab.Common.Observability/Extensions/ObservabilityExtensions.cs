using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Reflection;
using MassLab.Common.Observability.Configuration;

namespace MassLab.Common.Observability.Extensions;

/// <summary>
/// Service-collection &amp; application-builder extensions for OpenTelemetry.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracer + meter providers with ASP.NET Core,
    /// HttpClient, runtime, EFCore, gRPC client, and Redis instrumentation.
    /// Exports traces via OTLP and metrics via Prometheus by default.
    /// </summary>
    public static IServiceCollection AddMassLabObservability(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<ObservabilityOptions>? configureOptions = null,
        string sectionName = ObservabilityOptions.SectionName)
    {
        var opts = new ObservabilityOptions();
        configuration?.GetSection(sectionName).Bind(opts);
        configureOptions?.Invoke(opts);
        Validate(opts);

        if (configuration != null)
            services.Configure<ObservabilityOptions>(configuration.GetSection(sectionName));
        services.PostConfigure<ObservabilityOptions>(o => configureOptions?.Invoke(o));

        var version = opts.ServiceVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "1.0.0";

        var otel = services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService(serviceName: opts.ServiceName, serviceVersion: version);
                if (!string.IsNullOrWhiteSpace(opts.Environment))
                    r.AddAttributes(new[] { new KeyValuePair<string, object>("deployment.environment", opts.Environment!) });
            });

        if (opts.EnableTracing)
        {
            otel.WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation();
                t.AddHttpClientInstrumentation();
                if (opts.EnableEntityFrameworkCore) t.AddEntityFrameworkCoreInstrumentation();
                if (opts.EnableGrpcClient) t.AddGrpcClientInstrumentation();
                if (opts.EnableRedis && services.Any(s => s.ServiceType == typeof(IConnectionMultiplexer)))
                    t.AddRedisInstrumentation();
                t.AddOtlpExporter(o => o.Endpoint = new Uri(opts.OtlpEndpoint, UriKind.Absolute));
            });
        }

        if (opts.EnableMetrics)
        {
            otel.WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation();
                m.AddHttpClientInstrumentation();
                m.AddRuntimeInstrumentation();
                if (opts.EnablePrometheus) m.AddPrometheusExporter();
            });
        }

        return services;
    }

    /// <summary>
    /// Mounts the Prometheus scrape endpoint (default <c>/metrics</c>) when
    /// metrics + Prometheus are enabled.
    /// </summary>
    public static IApplicationBuilder UseMassLabPrometheus(this IApplicationBuilder app)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        return app;
    }

    /// <summary>
    /// Mounts the Prometheus scrape endpoint using the configured endpoint path.
    /// </summary>
    public static IApplicationBuilder UseMassLabPrometheus(
        this IApplicationBuilder app,
        IConfiguration configuration,
        string sectionName = ObservabilityOptions.SectionName)
    {
        var options = configuration.GetSection(sectionName).Get<ObservabilityOptions>() ?? new ObservabilityOptions();
        Validate(options);
        app.UseOpenTelemetryPrometheusScrapingEndpoint(options.PrometheusEndpoint);
        return app;
    }

    private static void Validate(ObservabilityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceName))
            throw new ArgumentException("Service name is required.", nameof(options.ServiceName));
        if (!Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out _))
            throw new ArgumentException("OTLP endpoint must be an absolute URI.", nameof(options.OtlpEndpoint));
        if (string.IsNullOrWhiteSpace(options.PrometheusEndpoint) || !options.PrometheusEndpoint.StartsWith('/'))
            throw new ArgumentException("Prometheus endpoint must start with '/'.", nameof(options.PrometheusEndpoint));
    }
}
