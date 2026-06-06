using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MassLab.Common.Authentication.ApiKey;
using MassLab.Common.Authentication.Configuration;
using MassLab.Common.Authentication.CurrentUser;
using MassLab.Common.Authentication.Extensions;
using MassLab.Common.Authentication.Tokens;

namespace MassLab.Common.Authentication.Tests;

public class AuthenticationTests
{
    [Fact]
    public void JwtTokenService_round_trips_signed_token()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "masslab",
            Audience = "tests",
            SigningKey = "0123456789abcdef0123456789abcdef"
        }));

        var token = service.GenerateToken(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())]));

        service.ValidateToken(token).Should().NotBeNull();
        service.GenerateRefreshToken().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CurrentUser_reads_claims_from_http_context()
    {
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("email", "dev@example.com"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("scope", "read write")
            ], "Bearer"))
        };

        var currentUser = new HttpContextCurrentUser(new HttpContextAccessor { HttpContext = context });

        currentUser.UserId.Should().Be(userId);
        currentUser.Email.Should().Be("dev@example.com");
        currentUser.Roles.Should().Contain("admin");
        currentUser.Scopes.Should().BeEquivalentTo("read", "write");
        currentUser.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void CurrentUser_reads_common_jwt_role_and_scope_claims()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("roles", "admin operator"),
                new Claim("role", "support"),
                new Claim("scp", "products.read products.write"),
                new Claim("scope", "orders.read")
            ], "Bearer"))
        };

        var currentUser = new HttpContextCurrentUser(new HttpContextAccessor { HttpContext = context });

        currentUser.Roles.Should().BeEquivalentTo("admin", "operator", "support");
        currentUser.Scopes.Should().BeEquivalentTo("products.read", "products.write", "orders.read");
    }

    [Fact]
    public void Jwt_options_bind_minute_and_day_aliases()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "masslab",
                ["Jwt:Audience"] = "tests",
                ["Jwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
                ["Jwt:RequireHttpsMetadata"] = "false"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddJwtAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
        options.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(15));
        options.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(7));
        options.RequireHttpsMetadata.Should().BeFalse();
    }

    [Fact]
    public void Jwt_options_validation_rejects_short_signing_key()
    {
        var services = new ServiceCollection();
        services.AddJwtAuthentication(options =>
        {
            options.Issuer = "masslab";
            options.Audience = "tests";
            options.SigningKey = "short";
        });
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public async Task Api_key_validator_accepts_configured_service_key()
    {
        var services = new ServiceCollection();
        services.AddApiKeyAuthentication(options =>
        {
            options.ServiceName = "OrderApi";
            options.ApiKey = "outbound-key";
            options.ApiKeys["ProductApi"] = "product-secret";
        });
        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IApiKeyValidator>();

        var result = await validator.ValidateAsync("product-secret", "ProductApi");

        result.Succeeded.Should().BeTrue();
        result.ServiceName.Should().Be("ProductApi");
    }

    [Fact]
    public async Task Api_key_validator_rejects_wrong_service_key_pair()
    {
        var services = new ServiceCollection();
        services.AddApiKeyAuthentication(options =>
        {
            options.ApiKeys["ProductApi"] = "product-secret";
            options.ApiKeys["OrderApi"] = "order-secret";
        });
        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IApiKeyValidator>();

        var result = await validator.ValidateAsync("product-secret", "OrderApi");

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Api_key_validator_accepts_key_without_service_name_by_default()
    {
        var services = new ServiceCollection();
        services.AddApiKeyAuthentication(options =>
        {
            options.ApiKeys["ExternalPartner"] = "partner-secret";
        });
        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IApiKeyValidator>();

        var result = await validator.ValidateAsync("partner-secret", serviceName: null);

        result.Succeeded.Should().BeTrue();
        result.ServiceName.Should().Be("ExternalPartner");
    }

    [Fact]
    public void Api_key_options_validation_requires_at_least_one_key()
    {
        var services = new ServiceCollection();
        services.AddApiKeyAuthentication(_ => { });
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<ApiKeyOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }
}
