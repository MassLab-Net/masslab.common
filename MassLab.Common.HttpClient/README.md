# MassLab.Common.HttpClient

Typed HttpClient library with automatic traceId propagation, logging, and Polly resilience patterns for external API calls.

## Features

- **Typed HttpClient Registration**: register your own client interface and implementation with `IHttpClientFactory`
- **Automatic TraceId Propagation**: `TraceIdDelegatingHandler` adds X-Trace-Id header to all outgoing requests
- **Request/Response Logging**: `LoggingDelegatingHandler` logs HTTP method, URI, status code, and elapsed time
- **Retry Policy**: Polly retry with exponential backoff for transient HTTP errors (default 3 retries)
- **Circuit Breaker**: Polly circuit breaker opens after configured failures (default 5) for configured duration (default 30s)
- **API Key Propagation**: `ApiKeyDelegatingHandler` adds `X-API-Key` by default, or a custom header for named external/internal clients
- **Easy Registration**: Extension methods for DI registration with configurable resilience policies

## Usage

### Register a Typed HttpClient

```csharp
using MassLab.Common.HttpClient.Extensions;

// In Program.cs or Startup.cs
builder.Services.AddTypedHttpClient<IExternalApiClient, ExternalApiClient>(
    "https://api.external-service.com",
    enableRetry: true,
    enableCircuitBreaker: true);
```

### Register a Client with API Key

```csharp
builder.Services.AddApiKeyAuthentication(builder.Configuration);

builder.Services.AddTypedHttpClient<IProductClient, ProductClient>(
    "https://product-api",
    enableRetry: true,
    enableCircuitBreaker: true,
    enableApiKey: true);
```

The handler adds:

```http
X-API-Key: <ApiKey:ApiKey>
X-Service-Name: <ApiKey:ServiceName>
```

### Register an External Client with a Named API Key

```json
{
  "ApiKey": {
    "Clients": {
      "stripe": {
        "HeaderName": "X-Stripe-Key",
        "ApiKey": "stripe-secret",
        "Headers": {
          "X-Api-Version": "2026-01-01"
        }
      }
    }
  }
}
```

```csharp
builder.Services.AddTypedHttpClient<IStripeClient, StripeClient>(
    "https://api.stripe.com",
    enableApiKey: true,
    apiKeyClientName: "stripe");
```

### Implement a Typed HttpClient

```csharp
public interface IExternalApiClient
{
    Task<WeatherResponse> GetWeatherAsync(string city, CancellationToken cancellationToken = default);
}

public class ExternalApiClient : IExternalApiClient
{
    private readonly System.Net.Http.HttpClient _httpClient;

    public ExternalApiClient(System.Net.Http.HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherResponse> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/weather?city={city}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WeatherResponse>(cancellationToken);
    }
}
```

### Use the Typed HttpClient

```csharp
public class WeatherService
{
    private readonly IExternalApiClient _apiClient;

    public WeatherService(IExternalApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<WeatherResponse> GetWeatherAsync(string city)
    {
        // TraceId is automatically added to the request header
        // Request/response is automatically logged
        // Retry and circuit breaker policies are automatically applied
        return await _apiClient.GetWeatherAsync(city);
    }
}
```

## TraceId Propagation

The `TraceIdDelegatingHandler` automatically extracts the traceId from:
1. `HttpContext.Items["TraceId"]` (set by TraceIdMiddleware)
2. `Activity.Current?.Id` (from System.Diagnostics)
3. Generates a new GUID if neither is available

The traceId is added to all outgoing HTTP requests as the `X-Trace-Id` header.

## Logging

The `LoggingDelegatingHandler` logs:
- **Before request**: HTTP method and URI
- **After response**: HTTP status code, URI, and elapsed time in milliseconds

Example log output:
```
[12:34:56 INF] 00-abc123-def456-00 Sending HTTP GET request to https://api.external-service.com/weather?city=Seattle
[12:34:57 INF] 00-abc123-def456-00 Received HTTP 200 response from https://api.external-service.com/weather?city=Seattle in 1234ms
```

## Resilience Policies

### Retry Policy

Handles transient HTTP errors (5xx, 408, network failures) with exponential backoff:
- Retry 1: Wait 2 seconds
- Retry 2: Wait 4 seconds
- Retry 3: Wait 8 seconds

### Circuit Breaker Policy

Opens the circuit after 5 consecutive failures and keeps it open for 30 seconds. During this time, requests fail immediately without attempting the HTTP call.

## Configuration

You can customize retry and circuit breaker behavior:

```csharp
// Custom retry count
builder.Services.AddTypedHttpClient<IExternalApiClient, ExternalApiClient>(
    "https://api.external-service.com",
    enableRetry: true,
    enableCircuitBreaker: false);

// Or use Polly policies directly
builder.Services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.external-service.com");
})
.AddHttpMessageHandler<TraceIdDelegatingHandler>()
.AddHttpMessageHandler<LoggingDelegatingHandler>()
.AddPolicyHandler(PollyPolicies.GetRetryPolicy(retryCount: 5))
.AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy(exceptionsBeforeBreaking: 10, durationOfBreakInSeconds: 60));
```

## Requirements Validation

This library validates the following requirements:
- **19.1**: Typed HttpClient registration with a base address
- **19.2**: TraceIdDelegatingHandler adds X-Trace-Id header to outgoing requests
- **19.3**: TraceId extracted from HttpContext.Items["TraceId"] or Activity.Current?.Id
- **19.4**: Automatic traceId propagation in all HTTP requests
- **19.5**: Retry policy with exponential backoff for transient errors
- **19.6**: Circuit breaker opens after configured failures for configured duration
- **19.7**: ServiceCollectionExtensions for easy registration
- **19.8**: LoggingDelegatingHandler logs request method and URI
- **19.9**: LoggingDelegatingHandler logs response status code and elapsed time
- **19.10**: Support for configuring base address for typed clients
- **17.4**: TraceId propagation in outgoing HTTP requests
