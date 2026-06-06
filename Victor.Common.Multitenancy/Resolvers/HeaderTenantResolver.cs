using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Victor.Common.Multitenancy.Abstractions;
using Victor.Common.Multitenancy.Configuration;

namespace Victor.Common.Multitenancy.Resolvers;

/// <summary>Resolves tenant id from a request header.</summary>
public sealed class HeaderTenantResolver : ITenantResolver
{
    private readonly IOptions<MultitenancyOptions> _options;

    /// <summary>Creates the resolver.</summary>
    public HeaderTenantResolver(IOptions<MultitenancyOptions> options) => _options = options;

    /// <inheritdoc />
    public Task<Guid?> ResolveTenantIdAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var value = context.Request.Headers[_options.Value.HeaderName].ToString();
        return Task.FromResult<Guid?>(Guid.TryParse(value, out var tenantId) ? tenantId : null);
    }
}
