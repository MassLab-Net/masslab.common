# MassLab.Common.BackgroundJobs

Use this for background job abstractions: `IBackgroundJobScheduler`, `IBackgroundJob<TPayload>`, and recurring job bootstrapping. Always pair it with one provider.

NuGet:

```bash
dotnet add package MassLab.Common.BackgroundJobs
```

Define and register:

```csharp
public sealed record SendReceipt(Guid OrderId);

public sealed class SendReceiptJob : IBackgroundJob<SendReceipt>
{
    public Task ExecuteAsync(SendReceipt payload, CancellationToken ct) => Task.CompletedTask;
}

builder.Services.AddBackgroundJob<SendReceiptJob, SendReceipt>();
```

Use:

```csharp
await scheduler.EnqueueAsync(new SendReceipt(orderId), ct);
```

Read deeper only when needed: `MassLab.Common.BackgroundJobs/README.md`.

