namespace MassLab.Common.Authentication.Configuration;

/// <summary>
/// JWT authentication options bound from the <c>Jwt</c> configuration section.
/// </summary>
public class JwtOptions
{
    /// <summary>The default <c>IConfiguration</c> section name (<c>Jwt</c>).</summary>
    public const string SectionName = "Jwt";

    /// <summary>JWT issuer (<c>iss</c> claim).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>JWT audience (<c>aud</c> claim).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>OIDC/OAuth2 authority used to resolve OpenIddict metadata and JWKS.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>HMAC SHA-256 signing key. Min length 32 chars.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access-token lifetime (default 60 minutes).</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>Alternative access-token lifetime binding for simple JSON configuration.</summary>
    public int? AccessTokenLifetimeMinutes
    {
        get => (int)AccessTokenLifetime.TotalMinutes;
        set
        {
            if (value.HasValue)
                AccessTokenLifetime = TimeSpan.FromMinutes(value.Value);
        }
    }

    /// <summary>Refresh-token lifetime (default 30 days).</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Alternative refresh-token lifetime binding for simple JSON configuration.</summary>
    public int? RefreshTokenLifetimeDays
    {
        get => (int)RefreshTokenLifetime.TotalDays;
        set
        {
            if (value.HasValue)
                RefreshTokenLifetime = TimeSpan.FromDays(value.Value);
        }
    }

    /// <summary>Tolerance applied to token expiration (default 0).</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.Zero;

    /// <summary>If <c>true</c>, validates issuer, audience, lifetime, signing key.</summary>
    public bool ValidateAll { get; set; } = true;

    /// <summary>Requires HTTPS metadata when using authority-backed JWT bearer configuration.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
