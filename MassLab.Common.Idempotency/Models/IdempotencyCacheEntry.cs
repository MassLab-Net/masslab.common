namespace MassLab.Common.Idempotency.Models;

/// <summary>Cached HTTP response for an idempotent request.</summary>
public sealed class IdempotencyCacheEntry
{
    /// <summary>HTTP status code.</summary>
    public int StatusCode { get; set; }

    /// <summary>Content type returned by the original response.</summary>
    public string? ContentType { get; set; }

    /// <summary>Response body bytes.</summary>
    public byte[] Body { get; set; } = [];
}
