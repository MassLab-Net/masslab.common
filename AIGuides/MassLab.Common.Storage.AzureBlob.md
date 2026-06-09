# MassLab.Common.Storage.AzureBlob

Use this for Azure Blob Storage implementation of `IBlobStorage`.

NuGet:

```bash
dotnet add package MassLab.Common.Storage.AzureBlob
```

Setup:

```csharp
builder.Services.AddAzureBlobStorage(builder.Configuration);
```

Config section: `Storage:AzureBlob`. Use connection string or Azure service URI/default credentials.

Read deeper only when needed: `MassLab.Common.Storage.AzureBlob/README.md`.

