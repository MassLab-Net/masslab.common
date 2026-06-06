using Microsoft.AspNetCore.Http;

namespace Victor.Common.HttpClient.Handlers;

/// <summary>
/// Forwards the tenant id (from the inbound <c>X-Tenant-Id</c> header or
/// from <c>HttpContext.Items["TenantId"]</c>) to outgoing HTTP calls so
/// downstream services see the same tenant context.
/// </summary>
/// <remarks>
/// If no tenant id is available in the current request, the outgoing
/// request is sent unchanged.
/// </remarks>
public class TenantPropagationDelegatingHandler : DelegatingHandler
{
    private const string HeaderName = "X-Tenant-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance.</summary>
    public TenantPropagationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(HeaderName))
        {
            var ctx = _httpContextAccessor.HttpContext;
            string? tenantId = null;

            if (ctx is not null && ctx.Items.TryGetValue("TenantId", out var fromItems) && fromItems is not null)
                tenantId = fromItems.ToString();

            if (string.IsNullOrWhiteSpace(tenantId)
                && ctx is not null
                && ctx.Request.Headers.TryGetValue(HeaderName, out var fromHeader))
            {
                var headerValue = fromHeader.ToString();
                if (!string.IsNullOrWhiteSpace(headerValue))
                    tenantId = headerValue;
            }

            if (!string.IsNullOrWhiteSpace(tenantId))
                request.Headers.TryAddWithoutValidation(HeaderName, tenantId);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
