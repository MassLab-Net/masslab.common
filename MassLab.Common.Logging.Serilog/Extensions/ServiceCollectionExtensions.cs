using System.Reflection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Serilog.Sinks.OpenTelemetry;
using MassLab.Common.Logging.Configuration;
using MassLab.Common.Logging.Serilog.Enrichers;

namespace MassLab.Common.Logging.Serilog.Extensions;

/// <summary>
/// Extension methods for configuring Serilog logging.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Serilog with TraceId / UserId / TenantId enrichers and
    /// the configured sinks (Console, File, Seq, Application Insights,
    /// OpenTelemetry).
    /// </summary>
    public static IServiceCollection AddSerilogLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var loggingOptions = configuration.GetSection("Logging").Get<LoggingOptions>() ?? new LoggingOptions();
        var httpContextAccessor = new HttpContextAccessor();
        var logger = CreateLogger(loggingOptions, httpContextAccessor);
        Log.Logger = logger;

        services.Replace(ServiceDescriptor.Singleton<IHttpContextAccessor>(httpContextAccessor));

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger, dispose: true);
        });

        services.Replace(ServiceDescriptor.Singleton<DiagnosticContext, DiagnosticContext>());
        services.Replace(ServiceDescriptor.Singleton(logger));

        return services;
    }

    private static global::Serilog.ILogger CreateLogger(
        LoggingOptions loggingOptions,
        IHttpContextAccessor httpContextAccessor)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(ParseLogLevel(loggingOptions.MinimumLevel))
            .Enrich.FromLogContext()
            .Enrich.With(new TraceIdEnricher(httpContextAccessor))
            .Enrich.With(new UserIdEnricher(httpContextAccessor))
            .Enrich.With(new TenantIdEnricher(httpContextAccessor));

        foreach (var (key, value) in loggingOptions.MinimumLevelOverrides)
            loggerConfiguration.MinimumLevel.Override(key, ParseLogLevel(value));

        if (loggingOptions.EnableConsole)
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {Message:lj}{NewLine}{Exception}");
        }

        if (loggingOptions.EnableFile && !string.IsNullOrWhiteSpace(loggingOptions.FilePath))
        {
            loggerConfiguration.WriteTo.File(
                loggingOptions.FilePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {TraceId} {Message:lj}{NewLine}{Exception}");
        }

        if (loggingOptions.EnableSeq && !string.IsNullOrWhiteSpace(loggingOptions.SeqUrl))
            loggerConfiguration.WriteTo.Seq(loggingOptions.SeqUrl);

        if (loggingOptions.EnableApplicationInsights && !string.IsNullOrWhiteSpace(loggingOptions.ApplicationInsightsKey))
        {
            loggerConfiguration.WriteTo.ApplicationInsights(
                loggingOptions.ApplicationInsightsKey,
                TelemetryConverter.Traces);
        }

        if (loggingOptions.EnableOpenTelemetry)
        {
            var protocol = string.Equals(loggingOptions.OpenTelemetryProtocol, "HttpProtobuf",
                StringComparison.OrdinalIgnoreCase)
                ? OtlpProtocol.HttpProtobuf
                : OtlpProtocol.Grpc;
            var serviceName = loggingOptions.ServiceName
                              ?? Assembly.GetEntryAssembly()?.GetName().Name
                              ?? "masslab-service";

            loggerConfiguration.WriteTo.OpenTelemetry(o =>
            {
                o.Endpoint = loggingOptions.OpenTelemetryEndpoint;
                o.Protocol = protocol;
                o.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                };
            });
        }

        return loggerConfiguration.CreateLogger();
    }

    private static LogEventLevel ParseLogLevel(string level) => level.ToLowerInvariant() switch
    {
        "debug" => LogEventLevel.Debug,
        "information" => LogEventLevel.Information,
        "warning" => LogEventLevel.Warning,
        "error" => LogEventLevel.Error,
        "fatal" => LogEventLevel.Fatal,
        "none" => LogEventLevel.Fatal,
        _ => LogEventLevel.Information,
    };
}
