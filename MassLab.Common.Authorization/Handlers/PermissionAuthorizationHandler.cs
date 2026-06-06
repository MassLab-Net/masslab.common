using Microsoft.AspNetCore.Authorization;
using MassLab.Common.Authorization.Requirements;

namespace MassLab.Common.Authorization.Handlers;

/// <summary>
/// Authorizes a <see cref="PermissionRequirement"/> by inspecting the
/// principal's <c>permission</c> claim values.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Authorizes a <see cref="ScopeRequirement"/> by inspecting the principal's
/// space-delimited <c>scope</c> claim.
/// </summary>
public class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        var scopeClaims = context.User.Claims.Where(c => c.Type is "scope" or "scp");
        foreach (var claim in scopeClaims)
        {
            if (string.IsNullOrWhiteSpace(claim.Value)) continue;
            var scopes = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (scopes.Contains(requirement.Scope, StringComparer.Ordinal))
            {
                context.Succeed(requirement);
                break;
            }
        }
        return Task.CompletedTask;
    }
}
