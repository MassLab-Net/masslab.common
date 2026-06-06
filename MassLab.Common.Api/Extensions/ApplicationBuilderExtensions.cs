using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.Api.Middleware;
using MassLab.Common.Api.Models;

namespace MassLab.Common.Api.Extensions;

/// <summary>
/// Extension methods for configuring API middleware in the application pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds global exception handling middleware.
    /// Catches all unhandled exceptions and returns standardized
    /// <c>BaseApiResponse</c> or RFC 7807 <c>ProblemDetails</c> (configurable).
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        // Touch the factory once so the ambient resolver is wired.
        _ = app.ApplicationServices.GetService<BaseApiResponseFactory>();
        app.UseMiddleware<GlobalExceptionMiddleware>();
        return app;
    }

    /// <summary>Adds trace identifier middleware (run early in the pipeline).</summary>
    public static IApplicationBuilder UseTraceId(this IApplicationBuilder app)
    {
        app.UseMiddleware<TraceIdMiddleware>();
        return app;
    }

    /// <summary>Adds the per-request structured logging middleware (method/path/status/elapsedMs).</summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds standard security response headers (HSTS, X-Content-Type-Options,
    /// X-Frame-Options, Referrer-Policy, etc.).
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app,
        SecurityHeadersOptions? options = null)
    {
        if (options is null)
            app.UseMiddleware<SecurityHeadersMiddleware>();
        else
            app.UseMiddleware<SecurityHeadersMiddleware>(options);
        return app;
    }
}

/// <summary>
/// Service registration helpers for response compression.
/// </summary>
public static class CompressionServiceCollectionExtensions
{
    /// <summary>
    /// Registers Brotli + Gzip response compression with sensible defaults
    /// (enabled for HTTPS, JSON / text / xml MIME types).
    /// </summary>
    public static IServiceCollection AddMassLabResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true;
            o.Providers.Add<BrotliCompressionProvider>();
            o.Providers.Add<GzipCompressionProvider>();
            o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/problem+json",
                "application/xml",
                "text/json",
                "text/plain",
            });
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);
        return services;
    }
}
