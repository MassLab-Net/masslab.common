namespace MassLab.Common.Authentication.ApiKey;

/// <summary>
/// Defaults for API key authentication.
/// </summary>
public static class ApiKeyDefaults
{
    /// <summary>Authentication scheme name.</summary>
    public const string AuthenticationScheme = "ApiKey";

    /// <summary>Default header that carries the API key.</summary>
    public const string HeaderName = "X-API-Key";

    /// <summary>Legacy internal-service header that carries the API key.</summary>
    public const string InternalHeaderName = "X-Internal-Api-Key";

    /// <summary>Default header that identifies the calling client/service.</summary>
    public const string ServiceHeaderName = "X-Service-Name";

    /// <summary>Legacy internal-service header that identifies the calling service.</summary>
    public const string InternalServiceHeaderName = "X-Internal-Service";
}
