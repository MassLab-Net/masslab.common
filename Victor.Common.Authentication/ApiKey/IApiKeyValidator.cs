namespace Victor.Common.Authentication.ApiKey;

/// <summary>Validates API keys used by clients, services, or external partners.</summary>
public interface IApiKeyValidator
{
    /// <summary>Validates the supplied API key and optional caller service name.</summary>
    ValueTask<ApiKeyValidationResult> ValidateAsync(
        string apiKey,
        string? serviceName,
        CancellationToken cancellationToken = default);
}
