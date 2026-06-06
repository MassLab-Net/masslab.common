# MassLab.Common.Messaging.RabbitMQ

RabbitMQ provider for `MassLab.Common.Messaging`.

## Program.cs

```csharp
using MassLab.Common.Messaging.Extensions;
using MassLab.Common.Messaging.RabbitMQ.Extensions;

builder.Services.AddMassLabMessagingCore();
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
    "ExchangeName": "masslab.events"
  }
}
```

## Use in services

```csharp
await eventBus.PublishAsync(new ProductPriceChanged(productId, newPrice), ct);
```

The provider reuses broker connections/channels and supports dead-letter routing
for messages that cannot be processed successfully.
