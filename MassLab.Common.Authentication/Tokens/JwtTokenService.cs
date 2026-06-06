using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MassLab.Common.Authentication.Configuration;

namespace MassLab.Common.Authentication.Tokens;

/// <summary>
/// Issues and validates JWT access / refresh tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Issues a signed access token for the supplied identity.</summary>
    string GenerateToken(ClaimsIdentity identity, TimeSpan? lifetime = null);

    /// <summary>Issues a high-entropy refresh token (opaque string).</summary>
    string GenerateRefreshToken();

    /// <summary>Validates a token and returns its <see cref="ClaimsPrincipal"/>, or <c>null</c> on failure.</summary>
    ClaimsPrincipal? ValidateToken(string token);
}

/// <inheritdoc />
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey? _key;
    private readonly JwtSecurityTokenHandler _handler = new();

    /// <summary>Initializes a new instance.</summary>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        if (!string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            if (_options.SigningKey.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:SigningKey must be at least 32 characters long when configured.");

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        }
    }

    /// <inheritdoc />
    public string GenerateToken(ClaimsIdentity identity, TimeSpan? lifetime = null)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (_key is null)
            throw new InvalidOperationException(
                "Jwt:SigningKey is required to generate local HMAC tokens. Configure Jwt:SigningKey or issue tokens through the configured authority.");

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: identity.Claims,
            notBefore: now,
            expires: now.Add(lifetime ?? _options.AccessTokenLifetime),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
        return _handler.WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            return _handler.ValidateToken(token, BuildValidationParameters(), out _);
        }
        catch
        {
            return null;
        }
    }

    internal TokenValidationParameters BuildValidationParameters() => new()
    {
        ValidateIssuer = _options.ValidateAll,
        ValidateAudience = _options.ValidateAll,
        ValidateLifetime = _options.ValidateAll,
        ValidateIssuerSigningKey = _options.ValidateAll && _key is not null,
        ValidIssuer = _options.Issuer,
        ValidAudience = _options.Audience,
        IssuerSigningKey = _key,
        ClockSkew = _options.ClockSkew,
    };
}
