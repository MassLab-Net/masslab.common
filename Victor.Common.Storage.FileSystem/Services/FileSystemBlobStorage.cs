using Microsoft.Extensions.Options;
using Victor.Common.Storage;
using Victor.Common.Storage.Abstractions;
using Victor.Common.Storage.FileSystem.Configuration;
using Victor.Common.Storage.Models;

namespace Victor.Common.Storage.FileSystem.Services;

/// <summary>Stores blobs under a configured local root path.</summary>
public sealed class FileSystemBlobStorage : IBlobStorage
{
    private readonly FileSystemStorageOptions _options;

    /// <summary>Creates the storage provider.</summary>
    public FileSystemBlobStorage(IOptions<FileSystemStorageOptions> options)
    {
        _options = options.Value;
        ValidateOptions(_options);
    }

    /// <inheritdoc />
    public async Task<BlobInfo> UploadAsync(string container, string name, Stream content, BlobUploadOptions? options = null, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        var path = ResolvePath(container, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (File.Exists(path) && options?.Overwrite == false)
            throw new IOException($"Blob '{container}/{name}' already exists.");

        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);

        return new BlobInfo
        {
            Container = container,
            Name = name,
            Length = file.Length,
            ContentType = options?.ContentType,
            ETag = File.GetLastWriteTimeUtc(path).Ticks.ToString()
        };
    }

    /// <inheritdoc />
    public Task<BlobDownloadResult?> DownloadAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        var path = ResolvePath(container, name);
        if (!File.Exists(path))
            return Task.FromResult<BlobDownloadResult?>(null);

        // FileShare.Delete lets callers delete the underlying file even while the download stream is open,
        // matching the semantics of cloud blob providers (S3 / Azure Blob).
        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete);
        var info = new BlobInfo
        {
            Container = container,
            Name = name,
            Length = stream.Length,
            ETag = File.GetLastWriteTimeUtc(path).Ticks.ToString()
        };

        return Task.FromResult<BlobDownloadResult?>(new BlobDownloadResult(stream, info));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        var path = ResolvePath(container, name);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Development stub: returns a URL with an "expires" query parameter but performs
    /// no actual cryptographic signature or validation. Do NOT use in production for
    /// access-controlled resources without an additional authorization layer.
    /// </remarks>
    public Task<Uri> GetSignedUrlAsync(string container, string name, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        BlobStorageValidation.EnsurePositiveLifetime(lifetime);
        var escaped = $"{Uri.EscapeDataString(container)}/{Uri.EscapeDataString(name)}";
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return Task.FromResult(new Uri($"{_options.PublicBaseUrl.TrimEnd('/')}/{escaped}?expires={DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds()}"));

        return Task.FromResult(new UriBuilder
        {
            Scheme = Uri.UriSchemeFile,
            Path = Path.GetFullPath(ResolvePath(container, name))
        }.Uri);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string container, string name, CancellationToken ct = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        var path = ResolvePath(container, name);
        return Task.FromResult(File.Exists(path));
    }

    /// <inheritdoc />
    public Task<IEnumerable<BlobInfo>> ListAsync(string container, string? prefix = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(container))
            throw new ArgumentException("Container is required.", nameof(container));

        var root = Path.GetFullPath(Path.Combine(_options.RootPath, Sanitize(container)));
        if (!Directory.Exists(root))
            return Task.FromResult<IEnumerable<BlobInfo>>(Array.Empty<BlobInfo>());

        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories);
        var results = files
            .Select(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(name => prefix is null || name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(name => new BlobInfo
            {
                Container = container,
                Name = name,
                Length = new FileInfo(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar))).Length
            });
        return Task.FromResult<IEnumerable<BlobInfo>>(results.ToList());
    }

    private string ResolvePath(string container, string name)
    {
        var root = Path.GetFullPath(_options.RootPath);
        var path = Path.GetFullPath(Path.Combine(root, Sanitize(container), name.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            throw new InvalidOperationException("Blob path escapes the configured storage root.");
        return path;
    }

    private static string Sanitize(string value)
        => string.Join("_", value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

    private static void ValidateOptions(FileSystemStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RootPath))
            throw new ArgumentException("Root path is required.", nameof(options.RootPath));
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl)
            && !Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Public base URL must be an absolute URI.", nameof(options.PublicBaseUrl));
    }
}
