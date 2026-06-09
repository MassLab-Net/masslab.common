# MassLab.Common.Idempotency

Use this for idempotent HTTP endpoints using `Idempotency-Key` and cache-backed deduplication.

NuGet:

```bash
dotnet add package MassLab.Common.Idempotency
```

Setup:

```csharp
builder.Services.AddMassLabRedisCache(builder.Configuration.GetSection("Redis"));
builder.Services.AddMassLabIdempotency(builder.Configuration);

app.UseMassLabIdempotency();
```

Requires an `ICacheService` provider, normally Redis in production.

Read deeper only when needed: `MassLab.Common.Idempotency/README.md`.

