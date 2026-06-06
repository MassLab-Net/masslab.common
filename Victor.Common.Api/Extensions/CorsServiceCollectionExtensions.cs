using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Victor.Common.Api.Configuration;

namespace Victor.Common.Api.Extensions;

/// <summary>
/// Extension methods for configuring CORS services.
/// </summary>
public static class CorsServiceCollectionExtensions
{
    /// <summary>
    /// Adds CORS services with configuration from appsettings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="policyName">The CORS policy name (optional, uses default policy if not specified).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration,
        string? policyName = null)
    {
        var corsOptions = configuration.GetSection("Cors").Get<CorsOptions>() ?? new CorsOptions();

        services.AddCors(options =>
        {
            var configurePolicy = (Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy) =>
            {
                if (corsOptions.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(corsOptions.AllowedOrigins);
                }

                if (corsOptions.AllowCredentials)
                {
                    policy.AllowCredentials();
                }

                if (corsOptions.AllowedMethods.Contains("*"))
                {
                    policy.AllowAnyMethod();
                }
                else if (corsOptions.AllowedMethods.Length > 0)
                {
                    policy.WithMethods(corsOptions.AllowedMethods);
                }

                if (corsOptions.AllowedHeaders.Contains("*"))
                {
                    policy.AllowAnyHeader();
                }
                else if (corsOptions.AllowedHeaders.Length > 0)
                {
                    policy.WithHeaders(corsOptions.AllowedHeaders);
                }
            };

            if (string.IsNullOrEmpty(policyName))
            {
                options.AddDefaultPolicy(configurePolicy);
            }
            else
            {
                options.AddPolicy(policyName, configurePolicy);
            }
        });

        return services;
    }
}
