# MassLab.Common.Multitenancy

Use this for tenant resolution, tenant context, middleware, and EF Core tenant query filters.

NuGet:

```bash
dotnet add package MassLab.Common.Multitenancy
```

Setup:

```csharp
builder.Services.AddMassLabMultitenancy(builder.Configuration);

app.UseMassLabMultitenancy();
```

Config section: `Multitenancy`.

Use `ITenantContext` in services. For EF Core, implement tenant-aware context and apply tenant query filters for entities implementing `ITenantOwned`.

Read deeper only when needed: `MassLab.Common.Multitenancy/README.md`.

