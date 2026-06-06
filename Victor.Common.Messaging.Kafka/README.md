# Victor.Common.Messaging.Kafka

Kafka provider for `Victor.Common.Messaging`.

## Program.cs

```csharp
using Victor.Common.Messaging.Extensions;
using Victor.Common.Messaging.Kafka.Extensions;

builder.Services.AddVictorMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
builder.Services.AddKafkaEventBus(builder.Configuration);
```

## Configuration

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "product-api",
    "TopicPrefix": "victor"
  }
}
```

## Use in services

```csharp
await eventBus.PublishAsync(new ProductPriceChanged(productId, newPrice), ct);
```

Handlers are resolved from DI. Failed handler execution is retried and can be
routed to a dead-letter topic depending on provider options.
