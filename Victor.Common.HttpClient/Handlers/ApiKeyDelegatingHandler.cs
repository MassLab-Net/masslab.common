using Microsoft.Extensions.Options;
using Victor.Common.Authentication.ApiKey;

namespace Victor.Common.HttpClient.Handlers;

/// <summary>Adds the configured API key to outgoing service or external API calls.</summary>
public class ApiKeyDelegatingHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<ApiKeyOptions> _options;

    /// <summary>Named client configuration to use from <see cref="ApiKeyOptions.Clients"/>.</summary>
    public string? ApiKeyClientName { get; set; }

    /// <summary>Optional allowlist of hosts to receive the API key.</summary>
    public HashSet<string>? AllowedHosts { get; set; }

    /// <summary>Initializes a new instance.</summary>
    public ApiKeyDelegatingHandler(IOptionsMonitor<ApiKeyOptions> options)
        => _options = options;

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        var client = ResolveClientOptions(options);
        if (!ShouldAttach(request, client))
            return base.SendAsync(request, cancellationToken);

        var headerName = client.HeaderName!;
        if (!request.Headers.Contains(headerName))
            request.Headers.TryAddWithoutValidation(headerName, client.ApiKey);

        if (!string.IsNullOrWhiteSpace(client.ServiceName)
            && !string.IsNullOrWhiteSpace(client.ServiceHeaderName)
            && !request.Headers.Contains(client.ServiceHeaderName))
        {
            request.Headers.TryAddWithoutValidation(client.ServiceHeaderName, client.ServiceName);
        }

        foreach (var header in client.Headers)
        {
            if (!request.Headers.Contains(header.Key))
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return base.SendAsync(request, cancellationToken);
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
                AllowedHosts = configured.AllowedHosts,
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

    private bool ShouldAttach(HttpRequestMessage request, ApiKeyClientOptions client)
    {
        if (string.IsNullOrWhiteSpace(client.ApiKey) || string.IsNullOrWhiteSpace(client.HeaderName))
            return false;

        var allowedHosts = AllowedHosts ?? (client.AllowedHosts.Count > 0 ? client.AllowedHosts : null);
        return allowedHosts is null
               || (request.RequestUri is not null && allowedHosts!.Contains(request.RequestUri.Host));
    }
}
