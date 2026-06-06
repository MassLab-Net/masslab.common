using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Victor.Common.Authorization.Extensions;

namespace Victor.Common.Authorization.Tests;

public class AuthorizationTests
{
    [Fact]
    public async Task Dynamic_permission_policy_authorizes_matching_permission_claim()
    {
        using var provider = BuildProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permission", "products.read")
        ], "Bearer"));

        var result = await authorization.AuthorizeAsync(user, resource: null, policyName: "permission:products.read");

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Dynamic_scope_policy_authorizes_scope_and_scp_claims()
    {
        using var provider = BuildProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scope", "orders.read"),
            new Claim("scp", "products.read products.write")
        ], "Bearer"));

        var scopeResult = await authorization.AuthorizeAsync(user, resource: null, policyName: "scope:orders.read");
        var scpResult = await authorization.AuthorizeAsync(user, resource: null, policyName: "scope:products.write");

        scopeResult.Succeeded.Should().BeTrue();
        scpResult.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Dynamic_policy_provider_returns_null_for_empty_dynamic_policy_names()
    {
        using var provider = BuildProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var permissionPolicy = await policyProvider.GetPolicyAsync("permission:");
        var scopePolicy = await policyProvider.GetPolicyAsync("scope:");

        permissionPolicy.Should().BeNull();
        scopePolicy.Should().BeNull();
    }

    [Fact]
    public async Task Victor_authorization_replaces_default_policy_provider_registered_earlier()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddVictorAuthorization();
        using var provider = services.BuildServiceProvider();

        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await policyProvider.GetPolicyAsync("permission:orders.read");

        policy.Should().NotBeNull();
    }

    [Fact]
    public void Require_attributes_reject_empty_values()
    {
        var permission = () => new RequirePermissionAttribute("");
        var scope = () => new RequireScopeAttribute(" ");

        permission.Should().Throw<ArgumentException>();
        scope.Should().Throw<ArgumentException>();
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVictorAuthorization();
        return services.BuildServiceProvider();
    }
}
