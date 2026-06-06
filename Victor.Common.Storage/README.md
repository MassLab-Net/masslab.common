# Victor.Common.Storage

Provider-agnostic blob storage abstraction. Add one provider package:
`FileSystem`, `S3`, or `AzureBlob`.

## Main abstraction

```csharp
public interface IBlobStorage
{
    Task<BlobInfo> UploadAsync(string container, string name, Stream content, BlobUploadOptions? options = null, CancellationToken ct = default);
    Task<BlobDownloadResult?> DownloadAsync(string container, string name, CancellationToken ct = default);
    Task DeleteAsync(string container, string name, CancellationToken ct = default);
    Task<Uri> GetSignedUrlAsync(string container, string name, TimeSpan lifetime, CancellationToken ct = default);
    Task<bool> ExistsAsync(string container, string name, CancellationToken ct = default);
    Task<IEnumerable<BlobInfo>> ListAsync(string container, string? prefix = null, CancellationToken ct = default);
}
```

## Use in services

```csharp
public sealed class ProductImageService(IBlobStorage storage)
{
    public async Task<Uri> UploadAsync(Guid productId, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        await storage.UploadAsync("products", $"{productId}/{file.FileName}", stream, cancellationToken: ct);
        return await storage.GetSignedUrlAsync("products", $"{productId}/{file.FileName}", TimeSpan.FromMinutes(15), ct);
    }
}
```

Register a concrete provider in `Program.cs`.
