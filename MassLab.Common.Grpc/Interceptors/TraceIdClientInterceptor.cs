using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;

namespace MassLab.Common.Grpc.Interceptors;

/// <summary>
/// gRPC client interceptor that adds traceId to outgoing calls.
/// </summary>
public class TraceIdClientInterceptor : Interceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceIdClientInterceptor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public TraceIdClientInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Intercepts an asynchronous unary call to add traceId metadata.
    /// </summary>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, AddTraceId(context));
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(AddTraceId(context));
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, AddTraceId(context));
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(AddTraceId(context));
    }

    private ClientInterceptorContext<TRequest, TResponse> AddTraceId<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var traceId = GetTraceId();
        var headers = CopyHeadersWithoutTraceId(context.Options.Headers);
        headers.Add("x-trace-id", traceId);
        return new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, context.Options.WithHeaders(headers));
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

    private static Metadata CopyHeadersWithoutTraceId(Metadata? source)
    {
        var headers = new Metadata();
        if (source is null)
            return headers;

        foreach (var entry in source)
        {
            if (string.Equals(entry.Key, "x-trace-id", StringComparison.OrdinalIgnoreCase))
                continue;

            if (entry.IsBinary)
                headers.Add(entry.Key, entry.ValueBytes);
            else
                headers.Add(entry.Key, entry.Value);
        }

        return headers;
    }
}
