using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Victor.Common.Api.Middleware;

/// <summary>
/// Middleware that captures or generates a trace identifier for request correlation.
/// </summary>
public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TraceIdMiddleware> _logger;
    private const string TraceIdHeaderName = "X-Trace-Id";

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public TraceIdMiddleware(RequestDelegate next, ILogger<TraceIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to capture or generate trace identifier.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        string traceId;

        // Try to get traceId from header
        if (context.Request.Headers.TryGetValue(TraceIdHeaderName, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            traceId = headerValue.ToString();
        }
        else
        {
            // Generate new traceId
            traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        }

        // Store in HttpContext for access by other middleware/handlers
        context.Items["TraceId"] = traceId;

        // Add to response headers
        context.Response.Headers.Append(TraceIdHeaderName, traceId);

        // Add to logging scope
        using (_logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId }))
        {
            await _next(context);
        }
    }
}
