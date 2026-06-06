using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Victor.Common.Logging.Serilog.Enrichers;

/// <summary>
/// Enriches log events with the authenticated user id (<c>sub</c> or
/// <c>nameid</c> claim) when present in the current <see cref="HttpContext"/>.
/// </summary>
public class UserIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance.</summary>
    public UserIdEnricher(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return;

        var id = user.FindFirst("sub")?.Value
                 ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(id))
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("UserId", id));

        var name = user.FindFirst("preferred_username")?.Value
                   ?? user.FindFirst(ClaimTypes.Name)?.Value
                   ?? user.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("UserName", name));
    }
}

/// <summary>
/// Enriches log events with the current tenant id (from
/// <c>HttpContext.Items["TenantId"]</c> or the <c>X-Tenant-Id</c> header).
/// </summary>
public class TenantIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance.</summary>
    public TenantIdEnricher(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return;

        string? tenantId = null;
        if (ctx.Items.TryGetValue("TenantId", out var v) && v is not null)
            tenantId = v.ToString();
        else if (ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var h) && !string.IsNullOrWhiteSpace(h))
            tenantId = h.ToString();

        if (!string.IsNullOrWhiteSpace(tenantId))
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("TenantId", tenantId));
    }
}
