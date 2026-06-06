using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MassLab.Common.Authentication.CurrentUser;

/// <summary>
/// Abstraction exposing the authenticated user of the current request.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's id (parsed from <c>sub</c> or <c>nameid</c> claim).</summary>
    Guid UserId { get; }

    /// <summary>The user's display name (<c>preferred_username</c> or <c>name</c>).</summary>
    string? UserName { get; }

    /// <summary>The user's email (<c>email</c> claim).</summary>
    string? Email { get; }

    /// <summary>The user's roles.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>The user's scopes (<c>scope</c> claim, space-delimited).</summary>
    IReadOnlyList<string> Scopes { get; }

    /// <summary>True when an <see cref="HttpContext"/> with a verified principal is present.</summary>
    bool IsAuthenticated { get; }

    /// <summary>True when the user has a claim of the given type and value.</summary>
    bool HasClaim(string type, string value);
}

/// <inheritdoc />
public class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance.</summary>
    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid UserId
    {
        get
        {
            var raw = Principal?.FindFirst("sub")?.Value
                      ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }

    /// <inheritdoc />
    public string? UserName =>
        Principal?.FindFirst("preferred_username")?.Value
        ?? Principal?.FindFirst(ClaimTypes.Name)?.Value
        ?? Principal?.Identity?.Name;

    /// <inheritdoc />
    public string? Email =>
        Principal?.FindFirst("email")?.Value
        ?? Principal?.FindFirst(ClaimTypes.Email)?.Value;

    /// <inheritdoc />
    public IReadOnlyList<string> Roles =>
        Principal?.Claims
            .Where(c => c.Type is ClaimTypes.Role or "role" or "roles")
            .SelectMany(c => SplitClaimValues(c.Value, ' '))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
        ?? Array.Empty<string>();

    /// <inheritdoc />
    public IReadOnlyList<string> Scopes
    {
        get
        {
            return Principal?.Claims
                .Where(c => c.Type is "scope" or "scp")
                .SelectMany(c => SplitClaimValues(c.Value, ' '))
                .Distinct(StringComparer.Ordinal)
                .ToArray()
                ?? Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public bool HasClaim(string type, string value) =>
        Principal?.HasClaim(type, value) == true;

    private static IEnumerable<string> SplitClaimValues(string value, char separator) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
