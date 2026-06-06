using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Victor.Common.Logging.Abstractions;
using Victor.Common.Logging.Configuration;
using Victor.Common.Logging.Implementations;

namespace Victor.Common.Logging.Extensions;

/// <summary>
/// Extension methods for configuring common logging services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds common logging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommonLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure LoggingOptions from IConfiguration
        services.Configure<LoggingOptions>(configuration.GetSection("Logging"));

        services.TryAddTransient(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));

        return services;
    }
}
