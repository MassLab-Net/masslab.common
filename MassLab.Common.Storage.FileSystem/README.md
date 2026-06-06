# MassLab.Common.Storage.FileSystem

Local file-system provider for `IBlobStorage`. Useful for local development,
tests, and simple deployments.

## Program.cs

```csharp
using MassLab.Common.Storage.FileSystem.Extensions;

builder.Services.AddFileSystemBlobStorage(builder.Configuration);
```

## Configuration

```json
{
  "Storage": {
    "FileSystem": {
      "RootPath": "storage",
      "PublicBaseUrl": "https://localhost:5001/files"
    }
  }
}
```

## Use in services

```csharp
await storage.UploadAsync("avatars", $"{userId}.png", stream, cancellationToken: ct);
var exists = await storage.ExistsAsync("avatars", $"{userId}.png", ct);
```

`GetSignedUrlAsync` returns a URL using `PublicBaseUrl` when configured. Use a
cloud provider for real signed URLs in production.
