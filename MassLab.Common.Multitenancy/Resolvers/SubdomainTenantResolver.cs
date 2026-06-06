using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MassLab.Common.Multitenancy.Abstractions;
using MassLab.Common.Multitenancy.Configuration;

namespace MassLab.Common.Multitenancy.Resolvers;

/// <summary>Resolves a tenant from the first host label when configured as a Guid.</summary>
public sealed class SubdomainTenantResolver : ITenantResolver
{
    private readonly IOptions<MultitenancyOptions> _options;

    /// <summary>Creates the resolver.</summary>
    public SubdomainTenantResolver(IOptions<MultitenancyOptions> options) => _options = options;

    /// <inheritdoc />
    public Task<Guid?> ResolveTenantIdAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var host = context.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
            return Task.FromResult<Guid?>(null);

        string? subdomain;
        var baseDomain = _options.Value.BaseDomain?.Trim().Trim('.');
        if (!string.IsNullOrWhiteSpace(baseDomain))
        {
            if (string.Equals(host, baseDomain, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<Guid?>(null);

            var suffix = "." + baseDomain;
            if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<Guid?>(null);

            subdomain = host[..^suffix.Length].Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
        else
        {
            subdomain = host.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        return Task.FromResult<Guid?>(Guid.TryParse(subdomain, out var tenantId) ? tenantId : null);
    }
}
