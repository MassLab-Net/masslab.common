using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MassLab.Common.Api.Configuration;

namespace MassLab.Common.Api.Extensions;

/// <summary>
/// Extension methods for configuring health checks.
/// </summary>
public static class HealthCheckServiceCollectionExtensions
{
    /// <summary>
    /// Adds health check services with common configuration from appsettings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The health checks builder for further configuration.</returns>
    public static IHealthChecksBuilder AddCommonHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection("HealthCheck").Get<Configuration.HealthCheckOptions>() 
            ?? new Configuration.HealthCheckOptions();

        if (!options.Enabled)
        {
            return services.AddHealthChecks();
        }

        return services.AddHealthChecks();
    }

    /// <summary>
    /// Adds database health check using DbContext.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="name">The health check name (default: "database").</param>
    /// <param name="failureStatus">The failure status (default: Unhealthy).</param>
    /// <param name="tags">Optional tags for filtering.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddDatabaseHealthCheck<TContext>(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        string name = "database",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
        where TContext : DbContext
    {
        var options = configuration.GetSection("HealthCheck").Get<Configuration.HealthCheckOptions>() 
            ?? new Configuration.HealthCheckOptions();

        var timeout = TimeSpan.FromSeconds(options.Timeout);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new MassLab.Common.Api.HealthChecks.DbContextHealthCheck<TContext>(
                sp.GetRequiredService<TContext>(),
                timeout),
            failureStatus,
            tags ?? new[] { "db", "ready" },
            timeout));
    }
}
