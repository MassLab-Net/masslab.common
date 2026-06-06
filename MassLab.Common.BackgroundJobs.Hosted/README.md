# MassLab.Common.BackgroundJobs.Hosted

In-process background job provider for local development, tests, and small
services that do not need a durable external scheduler.

## Program.cs

```csharp
using MassLab.Common.BackgroundJobs.Hosted.Extensions;

builder.Services.AddMassLabHostedBackgroundJobs(builder.Configuration);
builder.Services.AddBackgroundJob<SendReceiptJob, SendReceipt>();
```

## Use in services

```csharp
public sealed class OrdersHandler(IBackgroundJobScheduler scheduler)
{
    public Task Handle(OrderCreated evt, CancellationToken ct)
        => scheduler.EnqueueAsync(new SendReceipt(evt.OrderId), ct);
}
```

Queued jobs run inside the current service process. Use Hangfire or Quartz when
jobs must survive process restarts or be coordinated across multiple instances.
