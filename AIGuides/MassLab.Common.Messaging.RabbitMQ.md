# MassLab.Common.Messaging.RabbitMQ

Use this for production event bus integration with RabbitMQ.

NuGet:

```bash
dotnet add package MassLab.Common.Messaging.RabbitMQ
```

Setup:

```csharp
builder.Services.AddMassLabMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
builder.Services.AddRabbitMqEventBus(builder.Configuration);
```

Config section: `RabbitMq` with host, credentials, exchange, retry/dead-letter settings.

Read deeper only when needed: `MassLab.Common.Messaging.RabbitMQ/README.md`.

