# MassLab.Common.Storage.S3

Use this for Amazon S3 or S3-compatible storage such as MinIO.

NuGet:

```bash
dotnet add package MassLab.Common.Storage.S3
```

Setup:

```csharp
builder.Services.AddS3BlobStorage(builder.Configuration);
```

Config section: `Storage:S3`. Leave explicit keys empty to use the default AWS credential chain.

Read deeper only when needed: `MassLab.Common.Storage.S3/README.md`.

