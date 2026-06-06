using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using MassLab.Common.Authentication.ApiKey;

namespace MassLab.Common.Grpc.Interceptors;

/// <summary>Adds the configured API key to outgoing gRPC calls.</summary>
public class ApiKeyClientInterceptor : Interceptor
{
    private readonly IOptionsMonitor<ApiKeyOptions> _options;

    /// <summary>Named client configuration to use from <see cref="ApiKeyOptions.Clients"/>.</summary>
    public string? ApiKeyClientName { get; set; }

    /// <summary>Initializes a new instance.</summary>
    public ApiKeyClientInterceptor(IOptionsMonitor<ApiKeyOptions> options)
        => _options = options;

    /// <inheritdoc />
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, AddApiKey(context));
    }

    /// <inheritdoc />
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(AddApiKey(context));
    }

    /// <inheritdoc />
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, AddApiKey(context));
    }

    /// <inheritdoc />
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(AddApiKey(context));
    }

    private ClientInterceptorContext<TRequest, TResponse> AddApiKey<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var options = _options.CurrentValue;
        var client = ResolveClientOptions(options);
        if (string.IsNullOrWhiteSpace(client.ApiKey) || string.IsNullOrWhiteSpace(client.HeaderName))
            return context;

        var headers = CopyHeaders(context.Options.Headers);
        AddIfMissing(headers, client.HeaderName, client.ApiKey);

        if (!string.IsNullOrWhiteSpace(client.ServiceName)
            && !string.IsNullOrWhiteSpace(client.ServiceHeaderName))
            AddIfMissing(headers, client.ServiceHeaderName, client.ServiceName);

        foreach (var header in client.Headers)
            AddIfMissing(headers, header.Key, header.Value);

        return new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, context.Options.WithHeaders(headers));
    }

    private ApiKeyClientOptions ResolveClientOptions(ApiKeyOptions options)
    {
        if (!string.IsNullOrWhiteSpace(ApiKeyClientName)
            && options.Clients.TryGetValue(ApiKeyClientName, out var configured))
        {
            return new ApiKeyClientOptions
            {
                HeaderName = string.IsNullOrWhiteSpace(configured.HeaderName)
                    ? options.HeaderName
                    : configured.HeaderName,
                ApiKey = configured.ApiKey,
                ServiceName = configured.ServiceName ?? options.ServiceName,
                ServiceHeaderName = string.IsNullOrWhiteSpace(configured.ServiceHeaderName)
                    ? options.ServiceHeaderName
                    : configured.ServiceHeaderName,
                Headers = configured.Headers
            };
        }

        return new ApiKeyClientOptions
        {
            HeaderName = options.HeaderName,
            ApiKey = options.ApiKey,
            ServiceName = options.ServiceName,
            ServiceHeaderName = options.ServiceHeaderName
        };
    }

    private static Metadata CopyHeaders(Metadata? source)
    {
        var headers = new Metadata();
        if (source is null)
            return headers;

        foreach (var entry in source)
        {
            if (entry.IsBinary)
                headers.Add(entry.Key, entry.ValueBytes);
            else
                headers.Add(entry.Key, entry.Value);
        }

        return headers;
    }

    private static void AddIfMissing(Metadata headers, string key, string value)
    {
        if (headers.Any(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)))
            return;

        headers.Add(key.ToLowerInvariant(), value);
    }
}
