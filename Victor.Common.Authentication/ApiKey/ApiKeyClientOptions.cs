namespace Victor.Common.Authentication.ApiKey;

/// <summary>Outbound API key settings for a named HTTP/gRPC client.</summary>
public class ApiKeyClientOptions
{
    /// <summary>Header that carries the API key for this client.</summary>
    public string? HeaderName { get; set; }

    /// <summary>API key used for this outbound client.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Name of the caller application, partner, or service.</summary>
    public string? ServiceName { get; set; }

    /// <summary>Header that carries <see cref="ServiceName"/>.</summary>
    public string? ServiceHeaderName { get; set; }

    /// <summary>Optional host allowlist for this API key.</summary>
    public HashSet<string> AllowedHosts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Additional static headers to attach with this API key.</summary>
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
