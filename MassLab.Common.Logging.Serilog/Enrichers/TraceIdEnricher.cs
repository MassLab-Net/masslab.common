using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace MassLab.Common.Logging.Serilog.Enrichers;

/// <summary>
/// Enriches log events with traceId from HttpContext or Activity.
/// </summary>
public class TraceIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceIdEnricher"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public TraceIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Enriches the log event with traceId property.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">The property factory.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Items.TryGetValue("TraceId", out var traceId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
        }
        else if (Activity.Current?.Id != null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", Activity.Current.Id));
        }
    }
}
