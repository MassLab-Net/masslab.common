using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Victor.Common.HttpClient.Handlers;

/// <summary>
/// Delegating handler that logs HTTP request method, URI, response status code, and elapsed time.
/// </summary>
public class LoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingDelegatingHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingDelegatingHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sends an HTTP request and logs request/response details.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending HTTP {Method} request to {Uri}", request.Method, request.RequestUri);

        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation(
            "Received HTTP {StatusCode} response from {Uri} in {ElapsedMs}ms",
            (int)response.StatusCode,
            request.RequestUri,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
