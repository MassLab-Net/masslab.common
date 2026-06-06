# Victor.Common.Storage.S3

Amazon S3 and S3-compatible provider for `IBlobStorage`.

## Program.cs

```csharp
using Victor.Common.Storage.S3.Extensions;

builder.Services.AddS3BlobStorage(builder.Configuration);
```

## Configuration

```json
{
  "Storage": {
    "S3": {
      "Region": "us-east-1",
      "AccessKey": "local-access-key",
      "SecretKey": "local-secret-key",
      "ServiceUrl": "http://localhost:9000",
      "ForcePathStyle": true
    }
  }
}
```

Leave `AccessKey` and `SecretKey` empty to use the default AWS credential chain.
Use `ServiceUrl` for S3-compatible storage such as MinIO.

## Use in services

```csharp
await storage.UploadAsync("exports", "daily/report.csv", csvStream, cancellationToken: ct);
var url = await storage.GetSignedUrlAsync("exports", "daily/report.csv", TimeSpan.FromMinutes(10), ct);
```
