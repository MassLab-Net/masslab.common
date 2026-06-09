# MassLab.Common.Outbox

Use this for transactional outbox in EF Core services. Domain events are stored in the same database transaction and dispatched through `IEventBus`.

NuGet:

```bash
dotnet add package MassLab.Common.Outbox
```

Setup:

```csharp
builder.Services.AddMassLabMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
builder.Services.AddInMemoryEventBus(); // or Kafka/RabbitMQ in production
builder.Services.AddOutbox<ApplicationDbContext>(builder.Configuration);
```

Config section: `Outbox`.

Use durable messaging provider for production cross-service delivery.

Read deeper only when needed: `MassLab.Common.Outbox/README.md`.

