namespace Victor.Common.Authentication.ApiKey;

/// <summary>Result of validating an API key.</summary>
public sealed record ApiKeyValidationResult(bool Succeeded, string? ServiceName)
{
    /// <summary>Failed validation result.</summary>
    public static ApiKeyValidationResult Failed { get; } = new(false, null);

    /// <summary>Creates a successful validation result.</summary>
    public static ApiKeyValidationResult Success(string? serviceName) => new(true, serviceName);
}
