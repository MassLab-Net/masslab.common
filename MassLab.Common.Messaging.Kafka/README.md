# MassLab.Common.Messaging.Kafka

Kafka provider for `MassLab.Common.Messaging`.

## Program.cs

```csharp
using MassLab.Common.Messaging.Extensions;
using MassLab.Common.Messaging.Kafka.Extensions;

builder.Services.AddMassLabMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
builder.Services.AddKafkaEventBus(builder.Configuration);
```

## Configuration

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "product-api",
    "TopicPrefix": "masslab"
  }
}
```

## Use in services

```csharp
await eventBus.PublishAsync(new ProductPriceChanged(productId, newPrice), ct);
```

Handlers are resolved from DI. Failed handler execution is retried and can be
routed to a dead-letter topic depending on provider options.
