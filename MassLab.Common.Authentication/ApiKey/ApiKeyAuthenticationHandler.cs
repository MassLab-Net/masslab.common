using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Authentication.ApiKey;

/// <summary>Authentication handler for API keys.</summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyOptions>
{
    private readonly IApiKeyValidator _validator;

    /// <summary>Initializes a new instance.</summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator validator)
        : base(options, logger, encoder)
        => _validator = validator;

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var apiKeyValues = default(Microsoft.Extensions.Primitives.StringValues);
        var hasApiKey = false;
        foreach (var headerName in Options.GetAcceptedHeaderNames().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Request.Headers.TryGetValue(headerName, out apiKeyValues))
            {
                hasApiKey = true;
                break;
            }
        }

        if (!hasApiKey)
            return AuthenticateResult.NoResult();

        var apiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.Fail("API key is empty.");

        Request.Headers.TryGetValue(Options.ServiceHeaderName, out var serviceValues);
        var serviceName = serviceValues.FirstOrDefault();

        var result = await _validator.ValidateAsync(apiKey, serviceName, Context.RequestAborted);
        if (!result.Succeeded)
            return AuthenticateResult.Fail("Invalid API key.");

        var claims = new List<Claim>
        {
            new("internal_service", "true"),
            new(ClaimTypes.AuthenticationMethod, ApiKeyDefaults.AuthenticationScheme)
        };

        if (!string.IsNullOrWhiteSpace(result.ServiceName))
        {
            claims.Add(new(ClaimTypes.NameIdentifier, result.ServiceName));
            claims.Add(new(ClaimTypes.Name, result.ServiceName));
            claims.Add(new("service", result.ServiceName));
        }

        var identity = new ClaimsIdentity(claims, ApiKeyDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyDefaults.AuthenticationScheme);
        return AuthenticateResult.Success(ticket);
    }
}
