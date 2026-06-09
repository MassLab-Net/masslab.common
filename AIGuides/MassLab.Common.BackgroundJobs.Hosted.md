# MassLab.Common.BackgroundJobs.Hosted

Use this for simple in-process background jobs in local development, tests, or small services. Jobs do not survive process restarts.

NuGet:

```bash
dotnet add package MassLab.Common.BackgroundJobs.Hosted
```

Setup:

```csharp
builder.Services.AddMassLabHostedBackgroundJobs(builder.Configuration);
builder.Services.AddBackgroundJob<SendReceiptJob, SendReceipt>();
```

Use `IBackgroundJobScheduler` from services.

Read deeper only when needed: `MassLab.Common.BackgroundJobs.Hosted/README.md`.

