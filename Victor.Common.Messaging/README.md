# Victor.Common.Messaging

Core abstractions for integration events and event buses. Add one provider:
`InMemory`, `RabbitMQ`, or `Kafka`.

## Define an event

```csharp
public sealed record ProductPriceChanged(Guid ProductId, decimal NewPrice) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
```

## Handle an event

```csharp
public sealed class ProductPriceChangedHandler : IIntegrationEventHandler<ProductPriceChanged>
{
    public Task HandleAsync(ProductPriceChanged @event, CancellationToken ct)
    {
        // update read model, notify another system, etc.
        return Task.CompletedTask;
    }
}
```

## Program.cs

```csharp
using Victor.Common.Messaging.Extensions;

builder.Services.AddVictorMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
```

## Publish from services

```csharp
public sealed class ProductService(IEventBus bus)
{
    public Task ChangePrice(Guid id, decimal price, CancellationToken ct)
        => bus.PublishAsync(new ProductPriceChanged(id, price), ct);
}
```
