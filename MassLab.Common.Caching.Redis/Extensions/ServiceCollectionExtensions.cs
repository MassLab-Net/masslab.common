using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using MassLab.Common.Caching.Abstractions;
using MassLab.Common.Caching.Exceptions;
using MassLab.Common.Caching.Redis.Configuration;
using MassLab.Common.Caching.Redis.HealthChecks;
using MassLab.Common.Caching.Redis.Serialization;

namespace MassLab.Common.Caching.Redis.Extensions;

/// <summary>
/// Extension methods for registering Redis cache services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Redis cache services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing RedisCacheOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMassLabRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RedisCacheOptions();
        configuration.Bind(options);
        ValidateOptions(options);
        
        // Register RedisCacheOptions with configuration binding
        services.Configure<RedisCacheOptions>(opt => configuration.Bind(opt));
        
        RegisterServices(services);

        return services;
    }

    /// <summary>
    /// Adds Redis cache services to the service collection with inline configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMassLabRedisCache(
        this IServiceCollection services,
        Action<RedisCacheOptions> configureOptions)
    {
        var options = new RedisCacheOptions();
        configureOptions(options);
        ValidateOptions(options);

        // Register RedisCacheOptions with configuration binding
        services.Configure(configureOptions);
        RegisterServices(services);

        return services;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<Lazy<IConnectionMultiplexer>>(sp =>
        {
            return new Lazy<IConnectionMultiplexer>(() =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisCacheOptions>>().Value;
                var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
                configOptions.ConnectTimeout = (int)options.ConnectTimeout.TotalMilliseconds;
                configOptions.SyncTimeout = (int)options.OperationTimeout.TotalMilliseconds;
                configOptions.AbortOnConnectFail = false;
                return ConnectionMultiplexer.ConnectAsync(configOptions).GetAwaiter().GetResult();
            });
        });

        // Register IConnectionMultiplexer by resolving the Lazy wrapper
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            sp.GetRequiredService<Lazy<IConnectionMultiplexer>>().Value);

        services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
        // Register ICacheService with RedisCacheService implementation
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Register IAdvancedCacheService by resolving ICacheService
        services.AddSingleton<IAdvancedCacheService>(sp =>
            (IAdvancedCacheService)sp.GetRequiredService<ICacheService>());
        
        // Register IDistributedLock by resolving ICacheService and casting
        services.AddSingleton<IDistributedLock>(sp => 
            (IDistributedLock)sp.GetRequiredService<ICacheService>());
    }

    private static void ValidateOptions(RedisCacheOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new CacheConfigurationException(
                nameof(RedisCacheOptions.ConnectionString),
                "ConnectionString cannot be null or whitespace.");
        }

        if (options.ConnectTimeout <= TimeSpan.Zero)
        {
            throw new CacheConfigurationException(
                nameof(RedisCacheOptions.ConnectTimeout),
                "ConnectTimeout must be greater than zero.");
        }

        if (options.OperationTimeout <= TimeSpan.Zero)
        {
            throw new CacheConfigurationException(
                nameof(RedisCacheOptions.OperationTimeout),
                "OperationTimeout must be greater than zero.");
        }
    }

    /// <summary>
    /// Adds Redis cache health check to the health checks builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name.</param>
    /// <param name="failureStatus">The failure status.</param>
    /// <param name="tags">Optional tags.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddRedisCacheHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "redis_cache",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddCheck<RedisCacheHealthCheck>(
            name,
            failureStatus ?? HealthStatus.Unhealthy,
            tags ?? new[] { "cache", "redis", "ready" });
    }
}
