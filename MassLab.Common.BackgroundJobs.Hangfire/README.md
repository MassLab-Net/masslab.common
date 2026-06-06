# MassLab.Common.BackgroundJobs.Hangfire

Hangfire provider for `MassLab.Common.BackgroundJobs`.

## Program.cs

```csharp
using MassLab.Common.BackgroundJobs.Hangfire.Extensions;

builder.Services.AddMassLabHangfire(builder.Configuration);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboardSafe("/jobs");
```

## Configuration

```json
{
  "BackgroundJobs": {
    "StorageConnectionString": "Host=localhost;Database=masslab_jobs;Username=postgres;Password=postgres"
  }
}
```

`UseHangfireDashboardSafe` adds an authorization filter by default. Register job
classes with `AddBackgroundJob<TJob, TPayload>()` from the core package and
inject `IBackgroundJobScheduler` into application services.
