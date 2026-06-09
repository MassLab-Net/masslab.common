# MassLab.Common.Storage

Use this for provider-agnostic blob storage.

NuGet:

```bash
dotnet add package MassLab.Common.Storage
```

Main abstraction:

```csharp
IBlobStorage
```

Typical operations: upload, download, delete, exists, list, signed URL.

Choose provider:

- `MassLab.Common.Storage.FileSystem`
- `MassLab.Common.Storage.S3`
- `MassLab.Common.Storage.AzureBlob`

Read deeper only when needed: `MassLab.Common.Storage/README.md`.

