# MassLab.Common.Caching.Redis

Use this for distributed Redis cache and distributed locks.

NuGet:

```bash
dotnet add package MassLab.Common.Caching.Redis
```

Setup:

```csharp
builder.Services.AddMassLabRedisCache(builder.Configuration.GetSection("Redis"));
```

Config section:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,abortConnect=false",
    "InstanceName": "product-api",
    "DefaultExpiration": "00:30:00"
  }
}
```

Distributed lock:

```csharp
await using var lease = await locks.AcquireAsync("jobs:reprice", TimeSpan.FromMinutes(5), ct);
if (lease is null) return;
```

Read deeper only when needed: `MassLab.Common.Caching.Redis/README.md`.

