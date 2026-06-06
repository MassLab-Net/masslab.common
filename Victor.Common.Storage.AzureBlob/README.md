# Victor.Common.Storage.AzureBlob

Azure Blob Storage provider for `IBlobStorage`.

## Program.cs

```csharp
using Victor.Common.Storage.AzureBlob.Extensions;

builder.Services.AddAzureBlobStorage(builder.Configuration);
```

## Configuration

```json
{
  "Storage": {
    "AzureBlob": {
      "ConnectionString": "UseDevelopmentStorage=true"
    }
  }
}
```

Or use managed identity/default Azure credentials:

```json
{
  "Storage": {
    "AzureBlob": {
      "ServiceUri": "https://myaccount.blob.core.windows.net"
    }
  }
}
```

## Use in services

```csharp
public sealed class ReportService(IBlobStorage storage)
{
    public Task<BlobDownloadResult?> DownloadAsync(string name, CancellationToken ct)
        => storage.DownloadAsync("reports", name, ct);
}
```
