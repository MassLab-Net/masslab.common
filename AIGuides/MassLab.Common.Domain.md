# MassLab.Common.Domain

Use this for domain model primitives and DDD-style building blocks.

NuGet:

```bash
dotnet add package MassLab.Common.Domain
```

Main types:

```csharp
Entity
AggregateRoot
ValueObject
IDomainEvent
IAuditable
ISoftDeletable
ITenantOwned
ISpecification<T>
```

Use this in domain projects. Keep infrastructure concerns in EFCore/Dapper packages.

Read deeper only when needed: `MassLab.Common.Domain/README.md`.

