# MassLab.Common.BackgroundJobs

Provider-agnostic abstractions for queued and recurring background jobs. Choose
one provider package for execution: hosted in-process, Hangfire, or Quartz.

## Define jobs

```csharp
public sealed record SendReceipt(Guid OrderId);

public sealed class SendReceiptJob : IBackgroundJob<SendReceipt>
{
    public Task ExecuteAsync(SendReceipt payload, CancellationToken ct)
    {
        // send email, publish event, call another service, etc.
        return Task.CompletedTask;
    }
}
```

## Register jobs

```csharp
using MassLab.Common.BackgroundJobs.Extensions;

builder.Services.AddBackgroundJob<SendReceiptJob, SendReceipt>();
```

## Use in services

```csharp
public sealed class OrderService(IBackgroundJobScheduler jobs)
{
    public Task QueueReceipt(Guid orderId, CancellationToken ct)
        => jobs.EnqueueAsync(new SendReceipt(orderId), ct);
}
```

## Recurring jobs

Implement `IRecurringJobBootstrapper` to register recurring jobs at startup, then
call `AddRecurringJobBootstrapper<T>()` or `AddRecurringJobBootstrappers()`.
