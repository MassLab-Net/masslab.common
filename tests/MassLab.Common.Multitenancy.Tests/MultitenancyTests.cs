using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MassLab.Common.Multitenancy.Abstractions;
using MassLab.Common.Multitenancy.Configuration;
using MassLab.Common.Multitenancy.Extensions;
using MassLab.Common.Multitenancy.Resolvers;

namespace MassLab.Common.Multitenancy.Tests;

public class MultitenancyTests
{
    [Fact]
    public async Task Header_resolver_reads_configured_tenant_header()
    {
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = tenantId.ToString();

        var resolver = new HeaderTenantResolver(Options.Create(new MultitenancyOptions()));

        (await resolver.ResolveTenantIdAsync(context)).Should().Be(tenantId);
    }

    [Fact]
    public void Tenant_context_tracks_current_tenant()
    {
        var tenantId = Guid.NewGuid();
        ITenantContext context = new TenantContext();

        context.SetTenant(tenantId);

        context.HasTenant.Should().BeTrue();
        context.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Tenant_context_rejects_empty_tenant_id()
    {
        ITenantContext context = new TenantContext();

        var act = () => context.SetTenant(Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
    }

    [Fact]
    public void AddMassLabMultitenancy_is_idempotent()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddMassLabMultitenancy(configuration);
        services.AddMassLabMultitenancy(configuration);

        services.Count(d => d.ServiceType == typeof(ITenantResolver)
                            && d.ImplementationType == typeof(HeaderTenantResolver))
            .Should().Be(1);
        services.Count(d => d.ServiceType == typeof(ITenantResolver)).Should().Be(3);
    }

    [Fact]
    public async Task Subdomain_resolver_requires_base_domain_boundary()
    {
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString($"{tenantId}.example.com.evil.test");
        var resolver = new SubdomainTenantResolver(Options.Create(new MultitenancyOptions
        {
            BaseDomain = "example.com"
        }));

        var resolved = await resolver.ResolveTenantIdAsync(context);

        resolved.Should().BeNull();
    }
}
