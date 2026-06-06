# MassLab.Common.Caching.Redis

Redis implementation of `ICacheService`, `IAdvancedCacheService`, and
`IDistributedLock`.

## Program.cs

```csharp
using MassLab.Common.Caching.Redis.Extensions;

builder.Services.AddMassLabRedisCache(
    builder.Configuration.GetSection("Redis"));
```

## Configuration

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,abortConnect=false",
    "InstanceName": "product-api",
    "DefaultExpiration": "00:30:00",
    "ConnectTimeout": "00:00:05",
    "OperationTimeout": "00:00:01"
  }
}
```

## Use in services

```csharp
public sealed class ProductQueryService(ICacheService cache)
{
    public Task<ProductDto?> GetCachedAsync(Guid id, CancellationToken ct)
        => cache.GetAsync<ProductDto>($"products:{id}", ct);
}
```

## Distributed lock

```csharp
public sealed class RepriceProductsJob(IDistributedLock locks)
{
    public async Task Run(CancellationToken ct)
    {
        await using var lease = await locks.AcquireAsync("jobs:reprice", TimeSpan.FromMinutes(5), ct);
        if (lease is null) return;

        // only one instance runs this block
    }
}
```

Connections are initialized lazily and use Redis reconnect behavior. Prefer
`InstanceName` to avoid key collisions between services sharing one Redis.
