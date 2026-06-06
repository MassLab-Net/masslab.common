using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using MassLab.Common.Storage;
using MassLab.Common.Storage.Abstractions;

namespace MassLab.Common.Storage.AzureBlob.Services;

/// <summary>Azure Blob Storage provider.</summary>
public sealed class AzureBlobStorage : IBlobStorage
{
    private readonly BlobServiceClient _client;

    /// <summary>Creates the provider.</summary>
    public AzureBlobStorage(BlobServiceClient client) => _client = client;

    /// <inheritdoc />
    public async Task<Common.Storage.Models.BlobInfo> UploadAsync(
        string container,
        string name,
        Stream content,
        Common.Storage.Models.BlobUploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        var blob = await GetBlobClientAsync(container, name, cancellationToken);
        if (options?.Overwrite == false && await blob.ExistsAsync(cancellationToken))
            throw new IOException($"Blob '{container}/{name}' already exists.");

        await blob.UploadAsync(content, overwrite: true, cancellationToken);

        if (!string.IsNullOrWhiteSpace(options?.ContentType))
            await blob.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = options.ContentType }, cancellationToken: cancellationToken);

        if (options?.Metadata.Count > 0)
            await blob.SetMetadataAsync(options.Metadata, cancellationToken: cancellationToken);

        var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        return ToBlobInfo(container, name, properties.Value);
    }

    /// <inheritdoc />
    public async Task<Common.Storage.Models.BlobDownloadResult?> DownloadAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        var blob = _client.GetBlobContainerClient(container).GetBlobClient(name);
        if (!await blob.ExistsAsync(cancellationToken))
            return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);

        return new Common.Storage.Models.BlobDownloadResult(download.Value.Content, new Common.Storage.Models.BlobInfo
        {
            Container = container,
            Name = name,
            Length = download.Value.Details.ContentLength,
            ContentType = download.Value.Details.ContentType,
            ETag = download.Value.Details.ETag.ToString()
        });
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        var blob = _client.GetBlobContainerClient(container).GetBlobClient(name);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<Uri> GetSignedUrlAsync(string container, string name, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        BlobStorageValidation.EnsurePositiveLifetime(lifetime);
        var blob = _client.GetBlobContainerClient(container).GetBlobClient(name);
        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException("Azure Blob signed URLs require credentials that can generate SAS tokens.");

        return Task.FromResult(blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(lifetime)));
    }

    private async Task<BlobClient> GetBlobClientAsync(string container, string name, CancellationToken cancellationToken)
    {
        var containerClient = _client.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return containerClient.GetBlobClient(name);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string container, string name, CancellationToken ct = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        var blob = _client.GetBlobContainerClient(container).GetBlobClient(name);
        return await blob.ExistsAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Common.Storage.Models.BlobInfo>> ListAsync(string container, string? prefix = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(container))
            throw new ArgumentException("Container is required.", nameof(container));

        var containerClient = _client.GetBlobContainerClient(container);
        var results = new List<Common.Storage.Models.BlobInfo>();
        await foreach (var item in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: ct))
        {
            results.Add(new Common.Storage.Models.BlobInfo
            {
                Container = container,
                Name = item.Name,
                Length = item.Properties.ContentLength ?? 0,
                ContentType = item.Properties.ContentType,
                ETag = item.Properties.ETag?.ToString()
            });
        }
        return results;
    }

    private static Common.Storage.Models.BlobInfo ToBlobInfo(string container, string name, BlobProperties properties)
        => new()
        {
            Container = container,
            Name = name,
            Length = properties.ContentLength,
            ContentType = properties.ContentType,
            ETag = properties.ETag.ToString()
        };
}
