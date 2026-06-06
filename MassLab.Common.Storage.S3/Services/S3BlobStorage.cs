using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using MassLab.Common.Storage;
using MassLab.Common.Storage.Abstractions;
using MassLab.Common.Storage.Models;

namespace MassLab.Common.Storage.S3.Services;

/// <summary>S3-backed blob storage provider.</summary>
public sealed class S3BlobStorage : IBlobStorage
{
    private readonly IAmazonS3 _client;

    /// <summary>Creates the provider.</summary>
    public S3BlobStorage(IAmazonS3 client) => _client = client;

    /// <inheritdoc />
    public async Task<BlobInfo> UploadAsync(string container, string name, Stream content, BlobUploadOptions? options = null, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        if (options?.Overwrite == false && await ExistsAsync(container, name, cancellationToken))
            throw new IOException($"Blob '{container}/{name}' already exists.");

        var request = new PutObjectRequest
        {
            BucketName = container,
            Key = name,
            InputStream = content,
            AutoCloseStream = false,
            ContentType = options?.ContentType
        };

        foreach (var (key, value) in options?.Metadata ?? [])
            request.Metadata[key] = value;

        var response = await _client.PutObjectAsync(request, cancellationToken);
        return new BlobInfo
        {
            Container = container,
            Name = name,
            Length = content.CanSeek ? content.Length : 0,
            ContentType = options?.ContentType,
            ETag = response.ETag
        };
    }

    /// <inheritdoc />
    public async Task<BlobDownloadResult?> DownloadAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        try
        {
            var response = await _client.GetObjectAsync(container, name, cancellationToken);

            return new BlobDownloadResult(response.ResponseStream, new BlobInfo
            {
                Container = container,
                Name = name,
                Length = response.Headers.ContentLength,
                ContentType = response.Headers.ContentType,
                ETag = response.ETag
            });
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        await _client.DeleteObjectAsync(container, name, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Uri> GetSignedUrlAsync(string container, string name, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        BlobStorageValidation.EnsurePositiveLifetime(lifetime);
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = container,
            Key = name,
            Expires = DateTime.UtcNow.Add(lifetime),
            Verb = HttpVerb.GET
        });

        return Task.FromResult(new Uri(url));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string container, string name, CancellationToken ct = default)
    {
        BlobStorageValidation.EnsureValidBlobReference(container, name);
        try
        {
            await _client.GetObjectMetadataAsync(container, name, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BlobInfo>> ListAsync(string container, string? prefix = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(container))
            throw new ArgumentException("Container is required.", nameof(container));

        var request = new ListObjectsV2Request { BucketName = container, Prefix = prefix };
        var response = await _client.ListObjectsV2Async(request, ct);
        return response.S3Objects.Select(o => new BlobInfo
        {
            Container = container,
            Name = o.Key,
            Length = o.Size ?? 0,
            ETag = o.ETag
        });
    }
}
