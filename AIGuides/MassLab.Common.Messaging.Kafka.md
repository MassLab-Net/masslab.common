# MassLab.Common.Messaging.Kafka

Use this for production event bus integration with Apache Kafka.

NuGet:

```bash
dotnet add package MassLab.Common.Messaging.Kafka
```

Setup:

```csharp
builder.Services.AddMassLabMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
builder.Services.AddKafkaEventBus(builder.Configuration);
```

Config section: `Kafka` with `BootstrapServers`, `GroupId`, and topic settings.

Read deeper only when needed: `MassLab.Common.Messaging.Kafka/README.md`.

