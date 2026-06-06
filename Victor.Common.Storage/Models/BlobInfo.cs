namespace Victor.Common.Storage.Models;

/// <summary>Metadata returned after uploading a blob.</summary>
public sealed class BlobInfo
{
    /// <summary>Container/bucket name.</summary>
    public required string Container { get; init; }

    /// <summary>Blob name/key.</summary>
    public required string Name { get; init; }

    /// <summary>Content length in bytes.</summary>
    public long Length { get; init; }

    /// <summary>Content type.</summary>
    public string? ContentType { get; init; }

    /// <summary>Entity tag or checksum when the provider supplies one.</summary>
    public string? ETag { get; init; }
}
