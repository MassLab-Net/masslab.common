# MassLab.Common.Caching.Memory

Use this for single-instance memory cache, local development, or tests. Do not use it for distributed locking or multi-instance shared cache.

NuGet:

```bash
dotnet add package MassLab.Common.Caching.Memory
```

Setup:

```csharp
builder.Services.AddMassLabMemoryCache(builder.Configuration.GetSection("Caching:Memory"));
```

Config section: `Caching:Memory`.

Limitations: Redis-specific binary/hash/list/set/sorted-set operations and distributed locks are not supported.

Read deeper only when needed: `MassLab.Common.Caching.Memory/README.md`.

