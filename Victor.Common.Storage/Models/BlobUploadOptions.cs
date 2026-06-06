namespace Victor.Common.Storage.Models;

/// <summary>Upload options shared by storage providers.</summary>
public sealed class BlobUploadOptions
{
    /// <summary>Content type to persist with the blob.</summary>
    public string? ContentType { get; set; }

    /// <summary>Provider metadata.</summary>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Overwrite an existing blob.</summary>
    public bool Overwrite { get; set; } = true;
}
