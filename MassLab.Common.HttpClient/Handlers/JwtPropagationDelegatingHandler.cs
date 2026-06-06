using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace MassLab.Common.HttpClient.Handlers;

/// <summary>
/// Forwards the inbound <c>Authorization</c> header from the current request
/// to outgoing HTTP calls, so JWT bearer tokens flow seamlessly between
/// services in a single user transaction.
/// </summary>
/// <remarks>
/// If the inbound request has no <c>Authorization</c> header, the outgoing
/// request is sent unchanged.
/// </remarks>
public class JwtPropagationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Optional allowlist of hosts to forward the Authorization header to.</summary>
    public HashSet<string>? AllowedHosts { get; set; }

    /// <summary>Initializes a new instance.</summary>
    public JwtPropagationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is not null
            && !request.Headers.Contains(HeaderNames.Authorization)
            && ctx.Request.Headers.TryGetValue(HeaderNames.Authorization, out var auth))
        {
            var authValue = auth.ToString();
            if (!string.IsNullOrWhiteSpace(authValue)
                && (AllowedHosts is null || (request.RequestUri is not null && AllowedHosts.Contains(request.RequestUri.Host))))
            {
                request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authValue);
            }
        }
        return base.SendAsync(request, cancellationToken);
    }
}
