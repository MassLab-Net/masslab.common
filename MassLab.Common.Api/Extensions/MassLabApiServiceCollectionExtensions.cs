using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MassLab.Common.Api.Configuration;
using MassLab.Common.Api.Models;

namespace MassLab.Common.Api.Extensions;

/// <summary>
/// Extension methods to register the MassLab.Common.Api services.
/// </summary>
public static class MassLabApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IHttpContextAccessor"/>, <see cref="BaseApiResponseFactory"/>,
    /// an <see cref="IStartupFilter"/> that wires the ambient resolver, and
    /// binds <see cref="ApiResponseOptions"/> from configuration section
    /// <see cref="ApiResponseOptions.SectionName"/>.
    /// </summary>
    public static IServiceCollection AddMassLabApi(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<BaseApiResponseFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, BaseApiResponseStartupFilter>());

        if (configuration != null)
            services.Configure<ApiResponseOptions>(configuration.GetSection(ApiResponseOptions.SectionName));
        else
            services.Configure<ApiResponseOptions>(_ => { });

        return services;
    }

    /// <summary>
    /// Registers <see cref="IHttpContextAccessor"/>, <see cref="BaseApiResponseFactory"/>,
    /// an <see cref="IStartupFilter"/> that wires the ambient resolver, and
    /// binds <see cref="ApiResponseOptions"/> using an in-line configurator.
    /// </summary>
    public static IServiceCollection AddMassLabApi(
        this IServiceCollection services,
        Action<ApiResponseOptions> configure)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<BaseApiResponseFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, BaseApiResponseStartupFilter>());
        services.Configure(configure);
        return services;
    }
}
