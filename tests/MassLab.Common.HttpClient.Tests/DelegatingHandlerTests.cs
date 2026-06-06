using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using MassLab.Common.Authentication.ApiKey;
using MassLab.Common.HttpClient.Extensions;
using MassLab.Common.HttpClient.Handlers;
using MassLab.Common.HttpClient.Policies;

namespace MassLab.Common.HttpClient.Tests;

public class DelegatingHandlerTests
{
    [Fact]
    public async Task Jwt_handler_forwards_inbound_authorization_header()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer token";
        var capture = new CaptureHandler();
        var handler = new JwtPropagationDelegatingHandler(new HttpContextAccessor { HttpContext = context })
        {
            InnerHandler = capture
        };

        await new HttpMessageInvoker(handler).SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test"), default);

        capture.Request!.Headers.GetValues(HeaderNames.Authorization).Should().Contain("Bearer token");
    }

    [Fact]
    public async Task Tenant_handler_forwards_tenant_from_context_items()
    {
        var tenantId = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = tenantId;
        var capture = new CaptureHandler();
        var handler = new TenantPropagationDelegatingHandler(new HttpContextAccessor { HttpContext = context })
        {
            InnerHandler = capture
        };

        await new HttpMessageInvoker(handler).SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test"), default);

        capture.Request!.Headers.GetValues("X-Tenant-Id").Should().Contain(tenantId);
    }

    [Fact]
    public async Task Trace_handler_preserves_existing_trace_header()
    {
        var context = new DefaultHttpContext();
        context.Items["TraceId"] = "from-context";
        var capture = new CaptureHandler();
        var handler = new TraceIdDelegatingHandler(new HttpContextAccessor { HttpContext = context })
        {
            InnerHandler = capture
        };
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        request.Headers.Add("X-Trace-Id", "existing");

        await new HttpMessageInvoker(handler).SendAsync(request, default);

        capture.Request!.Headers.GetValues("X-Trace-Id").Should().ContainSingle().Which.Should().Be("existing");
    }

    [Fact]
    public async Task Api_key_handler_adds_default_key_and_service_headers()
    {
        var capture = new CaptureHandler();
        var handler = new ApiKeyDelegatingHandler(Options.Create(new ApiKeyOptions
        {
            ServiceName = "OrderApi",
            ApiKey = "order-secret"
        }).ToMonitor())
        {
            InnerHandler = capture
        };

        await new HttpMessageInvoker(handler).SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test"), default);

        capture.Request!.Headers.GetValues(ApiKeyDefaults.HeaderName).Should().Contain("order-secret");
        capture.Request!.Headers.GetValues(ApiKeyDefaults.ServiceHeaderName).Should().Contain("OrderApi");
    }

    [Fact]
    public async Task Api_key_handler_uses_named_client_header_and_key()
    {
        var capture = new CaptureHandler();
        var handler = new ApiKeyDelegatingHandler(Options.Create(new ApiKeyOptions
        {
            Clients =
            {
                ["stripe"] = new ApiKeyClientOptions
                {
                    HeaderName = "X-Stripe-Key",
                    ApiKey = "stripe-secret",
                    Headers = { ["X-Api-Version"] = "2026-01-01" }
                }
            }
        }).ToMonitor())
        {
            ApiKeyClientName = "stripe",
            InnerHandler = capture
        };

        await new HttpMessageInvoker(handler).SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.stripe.test"), default);

        capture.Request!.Headers.GetValues("X-Stripe-Key").Should().Contain("stripe-secret");
        capture.Request!.Headers.GetValues("X-Api-Version").Should().Contain("2026-01-01");
        capture.Request!.Headers.Contains(ApiKeyDefaults.ServiceHeaderName).Should().BeFalse();
    }

    [Fact]
    public void AddTypedHttpClient_rejects_non_http_base_address()
    {
        var services = new ServiceCollection();

        var act = () => services.AddTypedHttpClient<ITestClient, TestClient>("ftp://example.test");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("baseAddress");
    }

    [Theory]
    [InlineData(-1)]
    public void Retry_policy_rejects_invalid_retry_count(int retryCount)
    {
        var act = () => PollyPolicies.GetRetryPolicy(retryCount);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("retryCount");
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private interface ITestClient { }

    private sealed class TestClient : ITestClient
    {
        public TestClient(System.Net.Http.HttpClient httpClient)
        {
        }
    }
}

internal static class OptionsTestExtensions
{
    public static IOptionsMonitor<T> ToMonitor<T>(this IOptions<T> options)
        where T : class
        => new StaticOptionsMonitor<T>(options.Value);

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
        where T : class
    {
        public StaticOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
