# MassLab.Common.BackgroundJobs.Quartz

Quartz provider for `MassLab.Common.BackgroundJobs`, intended for scheduled and
recurring workloads.

## Program.cs

```csharp
using MassLab.Common.BackgroundJobs.Quartz.Extensions;

builder.Services.AddMassLabQuartz(builder.Configuration);
builder.Services.AddBackgroundJob<RefreshCatalogJob, RefreshCatalog>();
```

## Use in services

```csharp
public sealed class CatalogAdminService(IBackgroundJobScheduler scheduler)
{
    public Task ScheduleRefresh(CancellationToken ct)
        => scheduler.ScheduleAsync(new RefreshCatalog(), DateTimeOffset.UtcNow.AddMinutes(5), ct);
}
```

Use `IConcurrentJob` only for jobs that may run concurrently. By default, jobs
are protected from accidental overlapping execution.
