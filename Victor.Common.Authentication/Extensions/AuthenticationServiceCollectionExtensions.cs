using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Victor.Common.Authentication.ApiKey;
using Victor.Common.Authentication.Configuration;
using Victor.Common.Authentication.CurrentUser;
using Victor.Common.Authentication.Tokens;

namespace Victor.Common.Authentication.Extensions;

/// <summary>
/// Service-collection extensions to register JWT authentication and
/// <see cref="ICurrentUser"/>.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers JwtBearer authentication, <see cref="IJwtTokenService"/> and
    /// <see cref="ICurrentUser"/>.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = JwtOptions.SectionName)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(ValidateJwtOptions, "Jwt options are invalid.")
            .ValidateOnStart();
        return AddJwtAuthenticationCore(services);
    }

    /// <summary>
    /// Registers JwtBearer authentication using an in-line configurator.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        Action<JwtOptions> configure)
    {
        services.AddOptions<JwtOptions>()
            .Configure(configure)
            .Validate(ValidateJwtOptions, "Jwt options are invalid.")
            .ValidateOnStart();
        return AddJwtAuthenticationCore(services);
    }

    /// <summary>
    /// Registers API key authentication for service-to-service or external API calls.
    /// </summary>
    public static IServiceCollection AddApiKeyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = ApiKeyOptions.SectionName,
        bool setAsDefaultScheme = false)
    {
        services.AddOptions<ApiKeyOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(ValidateApiKeyOptions, "API key options are invalid.")
            .ValidateOnStart();
        services.Configure<ApiKeyOptions>(
            ApiKeyDefaults.AuthenticationScheme,
            configuration.GetSection(sectionName));
        return AddApiKeyAuthenticationCore(services, setAsDefaultScheme);
    }

    /// <summary>
    /// Registers API key authentication for service-to-service or external API calls.
    /// </summary>
    public static IServiceCollection AddApiKeyAuthentication(
        this IServiceCollection services,
        Action<ApiKeyOptions> configure,
        bool setAsDefaultScheme = false)
    {
        services.AddOptions<ApiKeyOptions>()
            .Configure(configure)
            .Validate(ValidateApiKeyOptions, "API key options are invalid.")
            .ValidateOnStart();
        services.Configure(ApiKeyDefaults.AuthenticationScheme, configure);
        return AddApiKeyAuthenticationCore(services, setAsDefaultScheme);
    }

    private static IServiceCollection AddJwtAuthenticationCore(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IJwtTokenService, JwtTokenService>();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();

        services
            .AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.SaveToken = true;
                o.MapInboundClaims = false;
            });

        // Configure JwtBearerOptions once at startup, reading JwtOptions.
        // This avoids mutating shared singleton state per-request and prevents
        // any race condition under load.
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        services.AddAuthorization();
        return services;
    }

    private static IServiceCollection AddApiKeyAuthenticationCore(
        IServiceCollection services,
        bool setAsDefaultScheme)
    {
        services.TryAddSingleton<IApiKeyValidator, ConfigurationApiKeyValidator>();

        if (setAsDefaultScheme)
        {
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = ApiKeyDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = ApiKeyDefaults.AuthenticationScheme;
            })
            .AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(
                ApiKeyDefaults.AuthenticationScheme,
                _ => { });
        }
        else
        {
            services.AddAuthentication()
                .AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyDefaults.AuthenticationScheme,
                    _ => { });
        }

        services.AddAuthorization();
        return services;
    }

    private static bool ValidateJwtOptions(JwtOptions options)
    {
        var hasAuthority = !string.IsNullOrWhiteSpace(options.Authority);
        var hasSigningKey = !string.IsNullOrWhiteSpace(options.SigningKey);

        if (!hasAuthority && (!hasSigningKey || options.SigningKey.Length < 32))
            return false;

        if (hasSigningKey && options.SigningKey.Length < 32)
            return false;

        if (options.ValidateAll &&
            string.IsNullOrWhiteSpace(options.Audience))
            return false;

        if (options.ValidateAll &&
            !hasAuthority &&
            string.IsNullOrWhiteSpace(options.Issuer))
            return false;

        return options.AccessTokenLifetime > TimeSpan.Zero
               && options.RefreshTokenLifetime > TimeSpan.Zero
               && options.ClockSkew >= TimeSpan.Zero;
    }

    private static bool ValidateApiKeyOptions(ApiKeyOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.HeaderName)
            || string.IsNullOrWhiteSpace(options.ServiceHeaderName))
            return false;

        var hasSingleKey = !string.IsNullOrWhiteSpace(options.ApiKey);
        var hasKeyMap = options.ApiKeys.Count > 0
                        && options.ApiKeys.All(x =>
                            !string.IsNullOrWhiteSpace(x.Key)
                            && !string.IsNullOrWhiteSpace(x.Value));
        var hasClientKeys = options.Clients.Count > 0
                            && options.Clients.All(x =>
                                !string.IsNullOrWhiteSpace(x.Key)
                                && !string.IsNullOrWhiteSpace(x.Value.ApiKey));

        return hasSingleKey || hasKeyMap || hasClientKeys;
    }

    /// <summary>
    /// Wires <see cref="JwtBearerOptions"/> from the bound
    /// <see cref="JwtOptions"/> instance. Runs once per scheme at startup.
    /// </summary>
    internal sealed class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IOptions<JwtOptions> _jwtOptions;

        public ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
            => _jwtOptions = jwtOptions;

        public void Configure(JwtBearerOptions options) => Configure(string.Empty, options);

        public void Configure(string? name, JwtBearerOptions options)
        {
            // Only configure the default JwtBearer scheme.
            if (!string.IsNullOrEmpty(name)
                && !string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
                return;

            var opts = _jwtOptions.Value;

            options.RequireHttpsMetadata = opts.RequireHttpsMetadata;
            options.MapInboundClaims = false;

            if (!string.IsNullOrWhiteSpace(opts.Authority))
            {
                options.Authority = opts.Authority;
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = opts.ValidateAll,
                ValidateAudience = opts.ValidateAll,
                ValidateLifetime = opts.ValidateAll,
                ValidAudience = opts.Audience,
                ClockSkew = opts.ClockSkew,
            };

            if (!string.IsNullOrWhiteSpace(opts.Issuer))
            {
                validationParameters.ValidIssuer = opts.Issuer;
            }

            if (!string.IsNullOrWhiteSpace(opts.SigningKey))
            {
                validationParameters.ValidateIssuerSigningKey = opts.ValidateAll;
                validationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey));
            }

            options.TokenValidationParameters = validationParameters;
        }
    }
}
