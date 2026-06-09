# MassLab.Common.Database

Use this for repository, unit-of-work, and connection abstractions. Pair it with EFCore or Dapper provider packages.

NuGet:

```bash
dotnet add package MassLab.Common.Database
```

Main abstractions:

```csharp
IReadRepository<TEntity>
IWriteRepository<TEntity>
IUnitOfWork
IConnectionFactory
```

Use:

```csharp
var entity = await repository.GetByIdAsync(id, ct);
await writer.AddAsync(entity, ct);
await unitOfWork.SaveChangesAsync(ct);
```

Choose concrete provider package before wiring DI.

Read deeper only when needed: `MassLab.Common.Database/README.md`.

