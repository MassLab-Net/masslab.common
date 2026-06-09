# MassLab.Common.Storage.FileSystem

Use this for local file-system blob storage in development, tests, or simple deployments.

NuGet:

```bash
dotnet add package MassLab.Common.Storage.FileSystem
```

Setup:

```csharp
builder.Services.AddFileSystemBlobStorage(builder.Configuration);
```

Config section: `Storage:FileSystem` with `RootPath` and optional `PublicBaseUrl`.

Read deeper only when needed: `MassLab.Common.Storage.FileSystem/README.md`.

