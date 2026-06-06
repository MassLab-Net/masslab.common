using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MassLab.Common.Caching.Abstractions;
using MassLab.Common.Caching.Memory.Configuration;
using MassLab.Common.Caching.Memory.HealthChecks;

namespace MassLab.Common.Caching.Memory.Extensions;

/// <summary>
/// Extension methods for registering memory cache services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds in-memory cache services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing MemoryCacheOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMassLabMemoryCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration to options to read values
        var options = new MassLabMemoryCacheOptions();
        configuration.Bind(options);
        
        // Register MemoryCacheOptions with configuration binding
        services.Configure<MassLabMemoryCacheOptions>(opt => configuration.Bind(opt));
        
        // Configure Microsoft.Extensions.Caching.Memory with size limit and compaction
        services.AddMemoryCache(opt =>
        {
            if (options.SizeLimit.HasValue)
                opt.SizeLimit = options.SizeLimit.Value;
            if (options.CompactionPercentage > 0)
                opt.CompactionPercentage = options.CompactionPercentage;
        });

        // Register ICacheService with MemoryCacheService implementation
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }

    /// <summary>
    /// Adds in-memory cache services to the service collection with inline configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMassLabMemoryCache(
        this IServiceCollection services,
        Action<MassLabMemoryCacheOptions> configureOptions)
    {
        // Apply configuration to get values for Microsoft.Extensions.Caching.Memory
        var options = new MassLabMemoryCacheOptions();
        configureOptions(options);

        // Register MemoryCacheOptions with configuration binding
        services.Configure(configureOptions);
        
        // Configure Microsoft.Extensions.Caching.Memory with size limit and compaction
        services.AddMemoryCache(opt =>
        {
            if (options.SizeLimit.HasValue)
                opt.SizeLimit = options.SizeLimit.Value;
            if (options.CompactionPercentage > 0)
                opt.CompactionPercentage = options.CompactionPercentage;
        });

        // Register ICacheService with MemoryCacheService implementation
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }

    /// <summary>
    /// Adds in-memory cache services to the service collection with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMassLabMemoryCache(
        this IServiceCollection services)
    {
        return AddMassLabMemoryCache(services, _ => { });
    }

    /// <summary>
    /// Adds memory cache health check to the health checks builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name.</param>
    /// <param name="failureStatus">The failure status.</param>
    /// <param name="tags">Optional tags.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddMemoryCacheHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "memory_cache",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddCheck<MemoryCacheHealthCheck>(
            name,
            failureStatus ?? HealthStatus.Unhealthy,
            tags ?? new[] { "cache", "memory", "ready" });
    }
}
