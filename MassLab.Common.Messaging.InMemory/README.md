# MassLab.Common.Messaging.InMemory

In-process `IEventBus` provider for local development and tests. It dispatches
events to registered handlers through DI without an external broker.

## Program.cs

```csharp
using MassLab.Common.Messaging.Extensions;
using MassLab.Common.Messaging.InMemory.Extensions;

builder.Services.AddMassLabMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
builder.Services.AddInMemoryEventBus();
```

## Use in services

```csharp
public sealed class ProductService(IEventBus bus)
{
    public Task PublishChange(Guid productId, decimal price, CancellationToken ct)
        => bus.PublishAsync(new ProductPriceChanged(productId, price), ct);
}
```

Do not use this provider for cross-service production messaging; events are lost
when the process exits.
