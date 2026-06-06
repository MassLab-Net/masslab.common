using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Victor.Common.Authorization.Handlers;
using Victor.Common.Authorization.Requirements;

namespace Victor.Common.Authorization.Extensions;

/// <summary>
/// Convenience attribute requiring a permission claim. Equivalent to
/// <c>[Authorize(Policy = "permission:&lt;name&gt;")]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>Initializes the attribute with the given permission name.</summary>
    public RequirePermissionAttribute(string permission) : base($"permission:{Validate(permission)}") { }

    private static string Validate(string permission) =>
        string.IsNullOrWhiteSpace(permission)
            ? throw new ArgumentException("Permission cannot be empty.", nameof(permission))
            : permission;
}

/// <summary>
/// Convenience attribute requiring an OAuth scope. Equivalent to
/// <c>[Authorize(Policy = "scope:&lt;name&gt;")]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequireScopeAttribute : AuthorizeAttribute
{
    /// <summary>Initializes the attribute with the given scope name.</summary>
    public RequireScopeAttribute(string scope) : base($"scope:{Validate(scope)}") { }

    private static string Validate(string scope) =>
        string.IsNullOrWhiteSpace(scope)
            ? throw new ArgumentException("Scope cannot be empty.", nameof(scope))
            : scope;
}

/// <summary>
/// Service-collection extensions for permission / scope authorization.
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the permission &amp; scope handlers and adds dynamic policy
    /// providers for <c>permission:&lt;name&gt;</c> / <c>scope:&lt;name&gt;</c>.
    /// </summary>
    public static IServiceCollection AddVictorAuthorization(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ScopeAuthorizationHandler>());
        services.AddAuthorization();
        services.AddVictorAuthorizationPolicyProvider();
        return services;
    }

    private static void AddVictorAuthorizationPolicyProvider(this IServiceCollection services)
    {
        var existing = services.LastOrDefault(d => d.ServiceType == typeof(IAuthorizationPolicyProvider));
        if (existing is null)
        {
            services.AddSingleton<IAuthorizationPolicyProvider, VictorAuthorizationPolicyProvider>();
            return;
        }

        if (existing.ImplementationType == typeof(VictorAuthorizationPolicyProvider))
            return;

        if (existing.ImplementationType == typeof(DefaultAuthorizationPolicyProvider))
        {
            services.Remove(existing);
            services.AddSingleton<IAuthorizationPolicyProvider, VictorAuthorizationPolicyProvider>();
        }
    }
}

/// <summary>
/// Dynamic policy provider that materializes <c>permission:</c> /
/// <c>scope:</c> policies on demand.
/// </summary>
internal class VictorAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private const int MaxCachedDynamicPolicies = 1024;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, AuthorizationPolicy> _cache = new();

    /// <summary>Initializes a new instance.</summary>
    public VictorAuthorizationPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options) :
        base(options) { }

    /// <inheritdoc />
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (_cache.TryGetValue(policyName, out var cached))
            return cached;

        if (policyName.StartsWith("permission:", StringComparison.Ordinal))
        {
            var permission = policyName["permission:".Length..];
            if (string.IsNullOrWhiteSpace(permission))
                return null;

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            CachePolicy(policyName, policy);
            return policy;
        }

        if (policyName.StartsWith("scope:", StringComparison.Ordinal))
        {
            var scope = policyName["scope:".Length..];
            if (string.IsNullOrWhiteSpace(scope))
                return null;

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ScopeRequirement(scope))
                .Build();
            CachePolicy(policyName, policy);
            return policy;
        }

        return await base.GetPolicyAsync(policyName);
    }

    private void CachePolicy(string policyName, AuthorizationPolicy policy)
    {
        if (_cache.Count < MaxCachedDynamicPolicies)
            _cache.TryAdd(policyName, policy);
    }
}
