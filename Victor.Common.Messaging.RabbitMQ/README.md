# Victor.Common.Messaging.RabbitMQ

RabbitMQ provider for `Victor.Common.Messaging`.

## Program.cs

```csharp
using Victor.Common.Messaging.Extensions;
using Victor.Common.Messaging.RabbitMQ.Extensions;

builder.Services.AddVictorMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
builder.Services.AddRabbitMqEventBus(builder.Configuration);
```

## Configuration

```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "ExchangeName": "victor.events"
  }
}
```

## Use in services

```csharp
await eventBus.PublishAsync(new ProductPriceChanged(productId, newPrice), ct);
```

The provider reuses broker connections/channels and supports dead-letter routing
for messages that cannot be processed successfully.
