using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using Victor.Common.Api.Configuration;

namespace Victor.Common.Api.Extensions;

/// <summary>
/// Extension methods for configuring health check endpoints.
/// </summary>
public static class HealthCheckApplicationBuilderExtensions
{
    /// <summary>
    /// Maps health check endpoints with JSON response format from configuration.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configuration">The configuration (optional, reads from app configuration if not provided).</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder MapHealthCheckEndpoints(
        this IApplicationBuilder app,
        IConfiguration? configuration = null)
    {
        if (app is not WebApplication webApp)
        {
            return app;
        }

        configuration ??= webApp.Configuration;
        var options = configuration.GetSection("HealthCheck").Get<Configuration.HealthCheckOptions>() 
            ?? new Configuration.HealthCheckOptions();

        if (!options.Enabled)
        {
            return app;
        }

        // Main health check endpoint - checks all
        webApp.MapHealthChecks(options.Endpoints.Health, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });

        // Ready endpoint - checks if app is ready to receive traffic (includes DB, Redis, etc.)
        webApp.MapHealthChecks(options.Endpoints.Ready, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        });

        // Live endpoint - checks if app is alive (basic check, no dependencies)
        webApp.MapHealthChecks(options.Endpoints.Live, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false, // No checks, just returns healthy if app is running
            ResponseWriter = WriteHealthCheckResponse
        });

        return app;
    }

    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return context.Response.WriteAsync(result);
    }
}
