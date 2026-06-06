using Victor.Common.Storage.Models;

namespace Victor.Common.Storage.Abstractions;

/// <summary>Provider-agnostic blob storage abstraction.</summary>
public interface IBlobStorage
{
    /// <summary>Uploads a blob.</summary>
    Task<BlobInfo> UploadAsync(string container, string name, Stream content, BlobUploadOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Downloads a blob, or returns null when it does not exist.</summary>
    Task<BlobDownloadResult?> DownloadAsync(string container, string name, CancellationToken cancellationToken = default);

    /// <summary>Deletes a blob when present.</summary>
    Task DeleteAsync(string container, string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a time-limited URL for reading a blob.</summary>
    Task<Uri> GetSignedUrlAsync(string container, string name, TimeSpan lifetime, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a blob exists.</summary>
    Task<bool> ExistsAsync(string container, string name, CancellationToken ct = default);

    /// <summary>Lists blobs in a container, optionally filtered by prefix.</summary>
    Task<IEnumerable<BlobInfo>> ListAsync(string container, string? prefix = null, CancellationToken ct = default);
}
