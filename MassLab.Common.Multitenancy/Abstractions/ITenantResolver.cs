using Microsoft.AspNetCore.Http;

namespace MassLab.Common.Multitenancy.Abstractions;

/// <summary>Resolves a tenant id for an HTTP request.</summary>
public interface ITenantResolver
{
    /// <summary>Attempts to resolve a tenant id.</summary>
    Task<Guid?> ResolveTenantIdAsync(HttpContext context, CancellationToken cancellationToken = default);
}
