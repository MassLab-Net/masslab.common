using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.HttpClient.Handlers;
using MassLab.Common.HttpClient.Policies;

namespace MassLab.Common.HttpClient.Extensions;

/// <summary>
/// Extension methods for registering typed HTTP clients with tracing, logging,
/// resilience policies, and optional JWT/tenant propagation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a typed HTTP client with automatic traceId propagation, request
    /// logging, and optional retry / circuit-breaker / JWT / tenant handlers.
    /// </summary>
    public static IServiceCollection AddTypedHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        string baseAddress,
        bool enableRetry = true,
        bool enableCircuitBreaker = true,
        bool enableJwtPropagation = false,
        bool enableApiKey = false,
        bool enableTenantPropagation = false,
        string? apiKeyClientName = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var parsedBaseAddress)
            || (parsedBaseAddress.Scheme != Uri.UriSchemeHttp && parsedBaseAddress.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Base address must be an absolute HTTP or HTTPS URI.", nameof(baseAddress));
        }

        services.AddHttpContextAccessor();
        services.AddTransient<TraceIdDelegatingHandler>();
        services.AddTransient<LoggingDelegatingHandler>();
        if (enableJwtPropagation)    services.AddTransient<JwtPropagationDelegatingHandler>();
        if (enableApiKey)            services.AddTransient<ApiKeyDelegatingHandler>();
        if (enableTenantPropagation) services.AddTransient<TenantPropagationDelegatingHandler>();

        var clientBuilder = services.AddHttpClient<TClient, TImplementation>(client =>
        {
            client.BaseAddress = parsedBaseAddress;
        })
        .AddHttpMessageHandler<TraceIdDelegatingHandler>()
        .AddHttpMessageHandler<LoggingDelegatingHandler>();

        if (enableJwtPropagation)
            clientBuilder.AddHttpMessageHandler<JwtPropagationDelegatingHandler>();

        if (enableApiKey)
        {
            clientBuilder.AddHttpMessageHandler(sp =>
            {
                var handler = ActivatorUtilities.CreateInstance<ApiKeyDelegatingHandler>(sp);
                handler.ApiKeyClientName = apiKeyClientName;
                return handler;
            });
        }

        if (enableTenantPropagation)
            clientBuilder.AddHttpMessageHandler<TenantPropagationDelegatingHandler>();

        if (enableRetry)
            clientBuilder.AddPolicyHandler(PollyPolicies.GetRetryPolicy());

        if (enableCircuitBreaker)
            clientBuilder.AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

        return services;
    }
}
