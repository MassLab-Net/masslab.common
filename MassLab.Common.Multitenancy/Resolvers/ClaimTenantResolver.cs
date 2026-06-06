using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MassLab.Common.Multitenancy.Abstractions;
using MassLab.Common.Multitenancy.Configuration;

namespace MassLab.Common.Multitenancy.Resolvers;

/// <summary>Resolves tenant id from the authenticated user's claims.</summary>
public sealed class ClaimTenantResolver : ITenantResolver
{
    private readonly IOptions<MultitenancyOptions> _options;

    /// <summary>Creates the resolver.</summary>
    public ClaimTenantResolver(IOptions<MultitenancyOptions> options) => _options = options;

    /// <inheritdoc />
    public Task<Guid?> ResolveTenantIdAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var value = context.User.FindFirst(_options.Value.ClaimType)?.Value;
        return Task.FromResult<Guid?>(Guid.TryParse(value, out var tenantId) ? tenantId : null);
    }
}
