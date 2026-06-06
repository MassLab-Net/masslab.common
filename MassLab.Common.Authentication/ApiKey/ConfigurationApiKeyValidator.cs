using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Authentication.ApiKey;

/// <summary>Validates API keys from <see cref="ApiKeyOptions"/>.</summary>
public sealed class ConfigurationApiKeyValidator : IApiKeyValidator
{
    private readonly IOptionsMonitor<ApiKeyOptions> _options;

    /// <summary>Initializes a new instance.</summary>
    public ConfigurationApiKeyValidator(IOptionsMonitor<ApiKeyOptions> options)
        => _options = options;

    /// <inheritdoc />
    public ValueTask<ApiKeyValidationResult> ValidateAsync(
        string apiKey,
        string? serviceName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return ValueTask.FromResult(ApiKeyValidationResult.Failed);

        var options = _options.CurrentValue;
        if (options.RequireServiceName && string.IsNullOrWhiteSpace(serviceName))
            return ValueTask.FromResult(ApiKeyValidationResult.Failed);

        if (options.ApiKeys.Count > 0)
            return ValueTask.FromResult(ValidateConfiguredKeys(options, apiKey, serviceName));

        if (!string.IsNullOrWhiteSpace(options.ApiKey)
            && Matches(options, apiKey, options.ApiKey))
        {
            return ValueTask.FromResult(ApiKeyValidationResult.Success(serviceName ?? options.ServiceName));
        }

        return ValueTask.FromResult(ApiKeyValidationResult.Failed);
    }

    private static ApiKeyValidationResult ValidateConfiguredKeys(
        ApiKeyOptions options,
        string apiKey,
        string? serviceName)
    {
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            return options.ApiKeys.TryGetValue(serviceName, out var expected)
                && Matches(options, apiKey, expected)
                    ? ApiKeyValidationResult.Success(serviceName)
                    : ApiKeyValidationResult.Failed;
        }

        foreach (var pair in options.ApiKeys)
        {
            if (Matches(options, apiKey, pair.Value))
                return ApiKeyValidationResult.Success(pair.Key);
        }

        return ApiKeyValidationResult.Failed;
    }

    private static bool Matches(ApiKeyOptions options, string actualKey, string expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
            return false;

        var actual = options.StoreKeysAsSha256Hashes
            ? Sha256Hex(actualKey)
            : actualKey;
        var normalizedExpected = options.StoreKeysAsSha256Hashes
            ? expected.ToLowerInvariant()
            : expected;

        return FixedTimeEquals(actual, normalizedExpected);
    }

    private static string Sha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
               && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
