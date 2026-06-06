using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;

namespace MassLab.Common.Grpc.Interceptors;

/// <summary>
/// gRPC server interceptor that extracts traceId from incoming calls.
/// </summary>
public class TraceIdServerInterceptor : Interceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceIdServerInterceptor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public TraceIdServerInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Intercepts a unary server call to extract traceId from metadata.
    /// </summary>
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        SetTraceId(context);
        return await continuation(request, context);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        SetTraceId(context);
        return await continuation(requestStream, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        SetTraceId(context);
        await continuation(request, responseStream, context);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        SetTraceId(context);
        await continuation(requestStream, responseStream, context);
    }

    private void SetTraceId(ServerCallContext context)
    {
        var traceId = context.RequestHeaders.GetValue("x-trace-id") ?? Guid.NewGuid().ToString();
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items["TraceId"] = traceId;
        }
    }
}
