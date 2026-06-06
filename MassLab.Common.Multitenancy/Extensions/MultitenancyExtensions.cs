using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MassLab.Common.Multitenancy.Abstractions;
using MassLab.Common.Multitenancy.Configuration;
using MassLab.Common.Multitenancy.Middleware;
using MassLab.Common.Multitenancy.Resolvers;

namespace MassLab.Common.Multitenancy.Extensions;

/// <summary>Registration helpers for multitenancy.</summary>
public static class MultitenancyExtensions
{
    /// <summary>Registers tenant context and default resolvers.</summary>
    public static IServiceCollection AddMassLabMultitenancy(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = MultitenancyOptions.SectionName)
    {
        if (configuration is not null)
            services.Configure<MultitenancyOptions>(configuration.GetSection(sectionName));
        else
            services.Configure<MultitenancyOptions>(_ => { });

        services.TryAddScoped<ITenantContext, TenantContext>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ITenantResolver, HeaderTenantResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ITenantResolver, ClaimTenantResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ITenantResolver, SubdomainTenantResolver>());
        return services;
    }

    /// <summary>Adds tenant resolution middleware.</summary>
    public static IApplicationBuilder UseMassLabMultitenancy(this IApplicationBuilder app)
        => app.UseMiddleware<TenantResolutionMiddleware>();
}
