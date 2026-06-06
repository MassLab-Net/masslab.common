using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Victor.Common.Authentication.ApiKey;
using Victor.Common.Grpc.Extensions;
using Victor.Common.Grpc.Interceptors;

namespace Victor.Common.Grpc.Tests;

public class GrpcRegistrationTests
{
    [Fact]
    public void Server_registration_adds_trace_interceptor()
    {
        var services = new ServiceCollection();

        services.AddGrpcServerWithTracing();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<TraceIdServerInterceptor>().Should().NotBeNull();
    }

    [Fact]
    public async Task Client_interceptor_replaces_existing_trace_header()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        httpContextAccessor.HttpContext.Items["TraceId"] = "trace-123";
        var interceptor = new TraceIdClientInterceptor(httpContextAccessor);
        var headers = new Metadata
        {
            { "x-trace-id", "old" },
            { "x-other", "value" }
        };
        var context = new ClientInterceptorContext<string, string>(
            TestMethod,
            "localhost",
            new CallOptions(headers));
        Metadata? capturedHeaders = null;

        var call = interceptor.AsyncUnaryCall(
            "request",
            context,
            (_, nextContext) =>
            {
                capturedHeaders = nextContext.Options.Headers;
                return new AsyncUnaryCall<string>(
                    Task.FromResult("response"),
                    Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess,
                    () => new Metadata(),
                    () => { });
            });

        await call.ResponseAsync;

        capturedHeaders.Should().NotBeNull();
        capturedHeaders!.Where(x => x.Key == "x-trace-id").Should().ContainSingle()
            .Which.Value.Should().Be("trace-123");
        capturedHeaders!.GetValue("x-other").Should().Be("value");
    }

    [Fact]
    public async Task Api_key_client_interceptor_adds_internal_metadata()
    {
        var interceptor = new ApiKeyClientInterceptor(new StaticOptionsMonitor<ApiKeyOptions>(new ApiKeyOptions
        {
            ServiceName = "OrderApi",
            ApiKey = "order-secret"
        }));
        var context = new ClientInterceptorContext<string, string>(
            TestMethod,
            "localhost",
            new CallOptions(new Metadata { { "x-other", "value" } }));
        Metadata? capturedHeaders = null;

        var call = interceptor.AsyncUnaryCall(
            "request",
            context,
            (_, nextContext) =>
            {
                capturedHeaders = nextContext.Options.Headers;
                return new AsyncUnaryCall<string>(
                    Task.FromResult("response"),
                    Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess,
                    () => new Metadata(),
                    () => { });
            });

        await call.ResponseAsync;

        capturedHeaders.Should().NotBeNull();
        capturedHeaders!.GetValue(ApiKeyDefaults.HeaderName.ToLowerInvariant()).Should().Be("order-secret");
        capturedHeaders!.GetValue(ApiKeyDefaults.ServiceHeaderName.ToLowerInvariant()).Should().Be("OrderApi");
        capturedHeaders!.GetValue("x-other").Should().Be("value");
    }

    [Fact]
    public async Task Api_key_client_interceptor_uses_named_client_metadata()
    {
        var interceptor = new ApiKeyClientInterceptor(new StaticOptionsMonitor<ApiKeyOptions>(new ApiKeyOptions
        {
            Clients =
            {
                ["partner"] = new ApiKeyClientOptions
                {
                    HeaderName = "x-partner-key",
                    ApiKey = "partner-secret",
                    Headers = { ["x-api-version"] = "2026-01-01" }
                }
            }
        }))
        {
            ApiKeyClientName = "partner"
        };
        var context = new ClientInterceptorContext<string, string>(
            TestMethod,
            "localhost",
            new CallOptions());
        Metadata? capturedHeaders = null;

        var call = interceptor.AsyncUnaryCall(
            "request",
            context,
            (_, nextContext) =>
            {
                capturedHeaders = nextContext.Options.Headers;
                return new AsyncUnaryCall<string>(
                    Task.FromResult("response"),
                    Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess,
                    () => new Metadata(),
                    () => { });
            });

        await call.ResponseAsync;

        capturedHeaders.Should().NotBeNull();
        capturedHeaders!.GetValue("x-partner-key").Should().Be("partner-secret");
        capturedHeaders!.GetValue("x-api-version").Should().Be("2026-01-01");
    }

    private static readonly Method<string, string> TestMethod = new(
        MethodType.Unary,
        "test.Service",
        "Call",
        Marshallers.StringMarshaller,
        Marshallers.StringMarshaller);

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
        where T : class
    {
        public StaticOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
