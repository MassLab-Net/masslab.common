using Microsoft.AspNetCore.Authentication;

namespace MassLab.Common.Authentication.ApiKey;

/// <summary>
/// Options for API key authentication and outbound propagation.
/// </summary>
public class ApiKeyOptions : AuthenticationSchemeOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ApiKey";

    /// <summary>Header that carries the API key.</summary>
    public string HeaderName { get; set; } = ApiKeyDefaults.HeaderName;

    /// <summary>
    /// Additional inbound headers accepted by the authentication handler.
    /// </summary>
    public string[] AcceptedHeaderNames { get; set; } =
    [
        ApiKeyDefaults.HeaderName,
        ApiKeyDefaults.InternalHeaderName
    ];

    /// <summary>Header that carries the caller client/service name.</summary>
    public string ServiceHeaderName { get; set; } = ApiKeyDefaults.ServiceHeaderName;

    /// <summary>Name of the current client/service. Used for outbound calls.</summary>
    public string? ServiceName { get; set; }

    /// <summary>Default API key used for outbound calls.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Accepted inbound keys by client/service name. Values may be raw keys or
    /// SHA-256 hex hashes when <see cref="StoreKeysAsSha256Hashes"/> is true.
    /// </summary>
    public Dictionary<string, string> ApiKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Named outbound client API key settings.</summary>
    public Dictionary<string, ApiKeyClientOptions> Clients { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether configured keys are SHA-256 hex hashes instead of raw secrets.</summary>
    public bool StoreKeysAsSha256Hashes { get; set; }

    /// <summary>Whether the client/service-name header is required for inbound requests.</summary>
    public bool RequireServiceName { get; set; }

    /// <summary>Returns all configured inbound API-key header names.</summary>
    public IEnumerable<string> GetAcceptedHeaderNames()
    {
        yield return HeaderName;

        foreach (var headerName in AcceptedHeaderNames)
        {
            if (!string.IsNullOrWhiteSpace(headerName))
                yield return headerName;
        }
    }
}
