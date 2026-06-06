# Victor.Common.Database

Provider-agnostic repository, unit-of-work, and connection abstractions used by
EF Core and Dapper provider packages.

## Main abstractions

```csharp
public interface IReadRepository<TEntity>
public interface IReadRepository<TEntity, TKey>
public interface IWriteRepository<TEntity>
public interface IUnitOfWork
public interface IConnectionFactory
```

## Use in application services

```csharp
public sealed class GetProductHandler(IReadRepository<Product> products)
{
    public Task<Product?> Handle(Guid id, CancellationToken ct)
        => products.GetByIdAsync(id, ct);
}

public sealed class CreateProductHandler(
    IWriteRepository<Product> products,
    IUnitOfWork unitOfWork)
{
    public async Task<Guid> Handle(CreateProduct command, CancellationToken ct)
    {
        var product = new Product(command.Name, command.Price);
        await products.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return product.Id;
    }
}
```

Register concrete implementations from `Victor.Common.Database.EFCore`,
`Victor.Common.Database.Dapper`, or provider-specific packages.
