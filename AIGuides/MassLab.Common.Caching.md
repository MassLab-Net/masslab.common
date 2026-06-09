# MassLab.Common.Caching

Use this for cache abstractions that should work with memory or Redis providers.

NuGet:

```bash
dotnet add package MassLab.Common.Caching
```

Main interfaces:

```csharp
ICacheService
IAdvancedCacheService
IDistributedLock
ICacheSerializer
```

Typical usage:

```csharp
var user = await cache.GetAsync<UserDto>($"users:{id}", ct);
await cache.SetAsync($"users:{id}", user, cancellationToken: ct);
```

Choose a provider:

- `MassLab.Common.Caching.Memory`
- `MassLab.Common.Caching.Redis`

Read deeper only when needed: `MassLab.Common.Caching/README.md`.

