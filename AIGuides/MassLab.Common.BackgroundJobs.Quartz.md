# MassLab.Common.BackgroundJobs.Quartz

Use this for scheduled and recurring jobs backed by Quartz.NET.

NuGet:

```bash
dotnet add package MassLab.Common.BackgroundJobs.Quartz
```

Setup:

```csharp
builder.Services.AddMassLabQuartz(builder.Configuration);
builder.Services.AddBackgroundJob<RefreshCatalogJob, RefreshCatalog>();
```

Use:

```csharp
await scheduler.ScheduleAsync(new RefreshCatalog(), DateTimeOffset.UtcNow.AddMinutes(5), ct);
```

Read deeper only when needed: `MassLab.Common.BackgroundJobs.Quartz/README.md`.

