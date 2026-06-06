using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace MassLab.Common.HttpClient.Handlers;

/// <summary>
/// Delegating handler that adds X-Trace-Id header to outgoing HTTP requests for distributed tracing.
/// </summary>
public class TraceIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TraceIdHeaderName = "X-Trace-Id";

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceIdDelegatingHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor to retrieve traceId from current request.</param>
    public TraceIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Sends an HTTP request with X-Trace-Id header added.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(TraceIdHeaderName))
        {
            var traceId = GetTraceId();
            request.Headers.Add(TraceIdHeaderName, traceId);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private string GetTraceId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue("TraceId", out var traceId) == true)
        {
            return traceId?.ToString() ?? Guid.NewGuid().ToString();
        }

        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}
