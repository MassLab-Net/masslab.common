using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.Idempotency.Configuration;
using MassLab.Common.Idempotency.Filters;
using MassLab.Common.Idempotency.Middleware;

namespace MassLab.Common.Idempotency.Extensions;

/// <summary>Registration helpers for idempotent request handling.</summary>
public static class IdempotencyExtensions
{
    /// <summary>Registers idempotency services.</summary>
    public static IServiceCollection AddMassLabIdempotency(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = IdempotencyOptions.SectionName)
    {
        if (configuration is not null)
            services.Configure<IdempotencyOptions>(configuration.GetSection(sectionName));
        else
            services.Configure<IdempotencyOptions>(_ => { });

        services.AddScoped<IdempotentFilter>();
        return services;
    }

    /// <summary>Adds idempotency middleware.</summary>
    public static IApplicationBuilder UseMassLabIdempotency(this IApplicationBuilder app)
        => app.UseMiddleware<IdempotencyMiddleware>();
}
