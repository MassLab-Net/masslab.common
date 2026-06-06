# Victor.Common.Caching.Memory

In-memory caching provider for the Victor.Common caching system, providing fast local cache storage within a single application instance.

## Overview

Victor.Common.Caching.Memory implements the `ICacheService` interface using Microsoft.Extensions.Caching.Memory, providing high-performance in-memory caching for single-instance applications. This provider is ideal for development, testing, or scenarios where distributed caching is not required.

## Features

- **Fast Local Storage**: In-memory cache with minimal latency
- **Expiration Support**: Both absolute and sliding expiration strategies
- **Size Management**: Configurable size limits with automatic compaction
- **Health Monitoring**: Built-in health check for cache availability
- **Type Safety**: Strongly-typed cache operations with generic type parameters
- **Async API**: Fully asynchronous operations for consistency with Redis provider

## Limitations

This provider is designed for single-instance applications. The following Redis-specific features throw `NotSupportedException`:

- Binary operations (`SetBinaryAsync`, `GetBinaryAsync`)
- Hash operations (`HashSetAsync`, `HashGetAsync`, `HashGetAllAsync`, `HashDeleteAsync`)
- List operations (`ListPushAsync`, `ListPopAsync`, `ListRangeAsync`, `ListLengthAsync`)
- Set operations (`SetAddAsync`, `SetRemoveAsync`, `SetMembersAsync`, `SetContainsAsync`)
- Sorted set operations (`SortedSetAddAsync`, `SortedSetRemoveAsync`, `SortedSetRangeAsync`, `SortedSetScoreAsync`)
- Distributed locking (`IDistributedLock` interface)

**Note:** Global key operations (`GetGlobalAsync`, `SetGlobalAsync`) are supported but behave identically to regular operations since memory cache doesn't distinguish between scoped and global keys.

## Installation

```bash
dotnet add package Victor.Common.Caching.Memory
```

## Configuration

### MemoryCacheOptions

Configure the memory cache provider using `MemoryCacheOptions`:

```csharp
public class MemoryCacheOptions
{
    // Maximum size of the cache (optional)
    // When reached, compaction will occur
    public long? SizeLimit { get; set; }

    // Percentage of entries to remove during compaction
    // Value between 0.0 and 1.0 (default: 0.2 for 20%)
    public double CompactionPercentage { get; set; } = 0.2;

    // Default expiration time for cache entries (optional)
    public TimeSpan? DefaultExpiration { get; set; }
}
```

### Configuration from appsettings.json

**appsettings.json:**
```json
{
  "Caching": {
    "Memory": {
      "SizeLimit": 1024,
      "CompactionPercentage": 0.25,
      "DefaultExpiration": "00:30:00"
    }
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddVictorMemoryCache(
    builder.Configuration.GetSection("Caching:Memory"));
```

### Inline Configuration

```csharp
builder.Services.AddVictorMemoryCache(options =>
{
    options.SizeLimit = 1024;
    options.CompactionPercentage = 0.25;
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
});
```

## Service Registration

### Basic Registration

```csharp
using Victor.Common.Caching.Memory.Extensions;

// Register memory cache with configuration
builder.Services.AddVictorMemoryCache(
    builder.Configuration.GetSection("Caching:Memory"));

// Or with inline configuration
builder.Services.AddVictorMemoryCache(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
});
```

### With Health Checks

```csharp
using Victor.Common.Caching.Memory.Extensions;

// Register memory cache
builder.Services.AddVictorMemoryCache(
    builder.Configuration.GetSection("Caching:Memory"));

// Add health checks
builder.Services.AddHealthChecks()
    .AddMemoryCacheHealthCheck(
        name: "memory_cache",
        tags: new[] { "cache", "memory", "ready" });
```

## Usage Examples

### Basic Cache Operations

```csharp
using Victor.Common.Caching.Abstractions;

public class UserService
{
    private readonly ICacheService _cache;

    public UserService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        var cacheKey = $"user:{userId}";
        
        // Try to get from cache
        var user = await _cache.GetAsync<User>(cacheKey);
        
        if (user == null)
        {
            // Load from database
            user = await LoadUserFromDatabaseAsync(userId);
            
            if (user != null)
            {
                // Store in cache with 30-minute expiration
                await _cache.SetAsync(cacheKey, user, new CacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
            }
        }
        
        return user;
    }

    public async Task UpdateUserAsync(User user)
    {
        // Update database
        await SaveUserToDatabaseAsync(user);
        
        // Invalidate cache
        await _cache.RemoveAsync($"user:{user.Id}");
    }
}
```

### Expiration Strategies

```csharp
// Absolute expiration - expires at specific time
await _cache.SetAsync("key", value, new CacheEntryOptions
{
    AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
});

// Absolute expiration relative to now - expires after duration
await _cache.SetAsync("key", value, new CacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
});

// Sliding expiration - resets on each access
await _cache.SetAsync("key", value, new CacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromMinutes(15)
});

// Use default expiration from configuration
await _cache.SetAsync("key", value);
```

### Cache-Aside Pattern

```csharp
public class ProductService
{
    private readonly ICacheService _cache;
    private readonly IProductRepository _repository;

    public async Task<Product?> GetProductAsync(string productId)
    {
        var cacheKey = $"product:{productId}";
        
        // Check cache first
        var product = await _cache.GetAsync<Product>(cacheKey);
        if (product != null)
        {
            return product;
        }
        
        // Cache miss - load from repository
        product = await _repository.GetByIdAsync(productId);
        
        if (product != null)
        {
            // Store in cache with sliding expiration
            await _cache.SetAsync(cacheKey, product, new CacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(20)
            });
        }
        
        return product;
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string categoryId)
    {
        var cacheKey = $"products:category:{categoryId}";
        
        var products = await _cache.GetAsync<List<Product>>(cacheKey);
        if (products != null)
        {
            return products;
        }
        
        products = await _repository.GetByCategoryAsync(categoryId);
        
        // Cache list with 10-minute expiration
        await _cache.SetAsync(cacheKey, products, new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });
        
        return products;
    }
}
```

### Checking Key Existence

```csharp
public async Task<bool> IsUserCachedAsync(string userId)
{
    var cacheKey = $"user:{userId}";
    return await _cache.ExistsAsync(cacheKey);
}
```

## Health Check Integration

The memory cache health check performs a write/read/remove test to verify cache operations:

```csharp
// Startup configuration
builder.Services.AddHealthChecks()
    .AddMemoryCacheHealthCheck(
        name: "memory_cache",
        tags: new[] { "cache", "memory", "ready" });

// Map health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

**Health Check Behavior:**
- **Healthy**: Write/read/remove test succeeds
- **Unhealthy**: Test fails or throws exception

## Switching to Redis Provider

The memory cache provider is ideal for development and single-instance deployments. To switch to Redis for distributed caching, simply change the service registration:

**Before (Memory):**
```csharp
builder.Services.AddVictorMemoryCache(
    builder.Configuration.GetSection("Caching:Memory"));
```

**After (Redis):**
```csharp
builder.Services.AddVictorRedisCache(
    builder.Configuration.GetSection("Caching:Redis"));
```

Your application code using `ICacheService` remains unchanged. Update your configuration to include Redis settings:

```json
{
  "Caching": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "InstanceName": "myapp",
      "DefaultExpiration": "00:30:00"
    }
  }
}
```

## Performance Considerations

### Size Limits

Configure `SizeLimit` to prevent unbounded memory growth:

```csharp
builder.Services.AddVictorMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit to 1024 entries
    options.CompactionPercentage = 0.25; // Remove 25% when limit reached
});
```

### Compaction

When the cache reaches `SizeLimit`, automatic compaction removes the least recently used entries based on `CompactionPercentage`.

### Default Expiration

Set `DefaultExpiration` to automatically expire entries that don't specify expiration:

```csharp
builder.Services.AddVictorMemoryCache(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
});
```

## Error Handling

Memory cache operations are synchronous internally but exposed as async for API consistency. Errors are rare but can occur:

```csharp
try
{
    await _cache.SetAsync("key", value);
}
catch (ArgumentException ex)
{
    // Key was null or whitespace
    _logger.LogError(ex, "Invalid cache key");
}
catch (NotSupportedException ex)
{
    // Attempted to use Redis-specific operation
    _logger.LogError(ex, "Operation not supported by memory cache");
}
```

## Target Framework

- .NET 10.0

## Dependencies

- Victor.Common.Caching (base abstractions)
- Microsoft.Extensions.Caching.Memory
- Microsoft.Extensions.Diagnostics.HealthChecks
- Microsoft.Extensions.Options

## Related Packages

- [Victor.Common.Caching](../Victor.Common.Caching/README.md) - Base abstractions
- [Victor.Common.Caching.Redis](../Victor.Common.Caching.Redis/README.md) - Redis provider

## License

Copyright © Victor.Common
