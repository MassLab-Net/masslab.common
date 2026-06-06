using Microsoft.AspNetCore.Authorization;

namespace MassLab.Common.Authorization.Requirements;

/// <summary>
/// Requires the authenticated user to have the named permission claim.
/// Permission claims are emitted under the <c>permission</c> claim type.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>The permission name (e.g. <c>orders.read</c>).</summary>
    public string Permission { get; }

    /// <summary>Initializes a new instance.</summary>
    public PermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be empty.", nameof(permission));
        Permission = permission;
    }
}

/// <summary>
/// Requires the authenticated user's <c>scope</c> claim to include the
/// requested OAuth scope.
/// </summary>
public class ScopeRequirement : IAuthorizationRequirement
{
    /// <summary>The required scope (e.g. <c>orders:read</c>).</summary>
    public string Scope { get; }

    /// <summary>Initializes a new instance.</summary>
    public ScopeRequirement(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be empty.", nameof(scope));
        Scope = scope;
    }
}
