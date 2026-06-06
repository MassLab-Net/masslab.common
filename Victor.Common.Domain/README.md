# Victor.Common.Domain

Framework-free domain primitives for DDD-style services.

## Main types

- `Entity` and `AggregateRoot`
- `ValueObject`
- `IDomainEvent`
- `IAuditable`, `ISoftDeletable`, `ITenantOwned`
- `ISpecification<T>`

## Define an aggregate

```csharp
public sealed class Product : AggregateRoot, IAuditable, ISoftDeletable, ITenantOwned
{
    public string TenantId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }

    public void ChangePrice(decimal price)
    {
        Price = price;
        AddDomainEvent(new ProductPriceChanged(Id, price));
    }
}
```

## Define a domain event

```csharp
public sealed record ProductPriceChanged(Guid ProductId, decimal NewPrice) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
```

## Specifications

```csharp
public sealed class ActiveProductsSpec : ISpecification<Product>
{
    public Expression<Func<Product, bool>> Criteria => p => !p.IsDeleted;
}
```

Use these primitives in domain projects. Keep infrastructure concerns in EFCore,
Dapper, messaging, or outbox packages.
