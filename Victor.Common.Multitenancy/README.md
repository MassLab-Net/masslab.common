# Victor.Common.Multitenancy

Tenant resolution and tenant context for multi-tenant services.

## Program.cs

```csharp
using Victor.Common.Multitenancy.Extensions;

builder.Services.AddVictorMultitenancy(builder.Configuration);

var app = builder.Build();
app.UseVictorMultitenancy();
```

## Configuration

```json
{
  "Multitenancy": {
    "HeaderName": "X-Tenant-Id",
    "ClaimType": "tenant_id",
    "BaseDomain": "example.com",
    "RequireTenant": true
  }
}
```

## Use in services

```csharp
public sealed class TenantAwareService(ITenantContext tenant)
{
    public string CurrentTenant()
        => tenant.TenantId ?? throw new InvalidOperationException("Tenant is required.");
}
```

## EF Core query filters

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
    : DbContext(options), ITenantDbContext
{
    public string? TenantId => tenant.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyTenantQueryFilters(this);
}
```

Entities that implement `ITenantOwned` are filtered by the current tenant. The
tenant can be resolved from header, claim, or subdomain.
