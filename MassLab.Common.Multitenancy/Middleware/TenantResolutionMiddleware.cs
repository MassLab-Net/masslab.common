using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MassLab.Common.Multitenancy.Abstractions;
using MassLab.Common.Multitenancy.Configuration;

namespace MassLab.Common.Multitenancy.Middleware;

/// <summary>Resolves and stores the current tenant for each request.</summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Creates the middleware.</summary>
    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Runs the middleware.</summary>
    public async Task InvokeAsync(
        HttpContext context,
        IEnumerable<ITenantResolver> resolvers,
        ITenantContext tenantContext,
        IOptions<MultitenancyOptions> options)
    {
        foreach (var resolver in resolvers)
        {
            var tenantId = await resolver.ResolveTenantIdAsync(context, context.RequestAborted);
            if (tenantId.HasValue)
            {
                tenantContext.SetTenant(tenantId.Value);
                context.Items["TenantId"] = tenantId.Value.ToString();
                break;
            }
        }

        if (options.Value.RequireTenant && !tenantContext.HasTenant)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Tenant id is required.", context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
