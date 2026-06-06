# Victor.Common.Outbox

Transactional outbox support for EF Core services. Domain events are stored in
the same database transaction and dispatched asynchronously through
`IEventBus`.

## Program.cs

```csharp
using Victor.Common.Messaging.Extensions;
using Victor.Common.Messaging.InMemory.Extensions;
using Victor.Common.Outbox.Extensions;

builder.Services.AddVictorMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
builder.Services.AddInMemoryEventBus();
builder.Services.AddOutbox<ApplicationDbContext>(builder.Configuration);
```

## Configuration

```json
{
  "Outbox": {
    "PollingInterval": "00:00:05",
    "BatchSize": 100,
    "MaxAttempts": 10,
    "RetentionDays": 7
  }
}
```

## Use in services

Add domain events to aggregates, save with EF Core, and let the outbox
background service dispatch them. Use a durable event-bus provider for
cross-service production delivery.
