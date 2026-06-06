using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MassLab.Common.Api.Middleware;

/// <summary>
/// Middleware that emits a structured log entry per HTTP request containing
/// method, path, status code, elapsed milliseconds, and traceId.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestLoggingMiddleware"/>.
    /// </summary>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Invokes the middleware to time and log the request.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var traceId = context.Items["TraceId"]?.ToString()
                          ?? Activity.Current?.Id
                          ?? context.TraceIdentifier;

            _logger.Log(
                LevelForStatus(context.Response.StatusCode),
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms (traceId={TraceId})",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                traceId);
        }
    }

    private static LogLevel LevelForStatus(int status) =>
        status >= 500 ? LogLevel.Error :
        status >= 400 ? LogLevel.Warning :
                        LogLevel.Information;
}
