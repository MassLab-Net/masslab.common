# MassLab.Common.Caching

Base abstractions package for the MassLab.Common caching system, providing unified interfaces for both in-memory and distributed Redis caching.

## Overview

MassLab.Common.Caching defines the core contracts and models for cache operations, enabling developers to write cache-agnostic code that works seamlessly with any provider implementation. This package contains only interfaces and abstractions with no provider-specific dependencies.

## Architecture

The caching system follows a three-package architecture:

- **MassLab.Common.Caching** (this package): Base abstractions and interfaces
- **MassLab.Common.Caching.Memory**: In-memory caching implementation
- **MassLab.Common.Caching.Redis**: Distributed Redis caching implementation

This separation allows applications to switch between caching strategies without code changes, simply by changing the service registration.

## Core Interfaces

### ICacheService

The primary abstraction for cache operations, providing strongly-typed async methods for all cache operations.

#### Basic Operations

```csharp
// Get a value from cache
var user = await cacheService.GetAsync<User>("user:123");

// Set a value in cache
await cacheService.SetAsync("user:123", user);

// Set with expiration
await cacheService.SetAsync("user:123", user, new CacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
});

// Remove a value
await cacheService.RemoveAsync("user:123");

// Check if key exists
bool exists = await cacheService.ExistsAsync("user:123");
```

#### Global Keys

Global keys bypass the instance name prefix (Redis only) and are shared across all service instances:

```csharp
// Set a global configuration value
await cacheService.SetGlobalAsync("app:config", config);

// Get a global configuration value
var config = await cacheService.GetGlobalAsync<AppConfig>("app:config");
```

#### Binary Operations (Redis-specific)

Store and retrieve raw binary data:

```csharp
// Store binary data
byte[] imageData = File.ReadAllBytes("image.png");
await cacheService.SetBinaryAsync("image:123", imageData);

// Retrieve binary data
byte[]? data = await cacheService.GetBinaryAsync("image:123");
```

**Note:** Memory cache provider throws `NotSupportedException` for binary operations.

#### Hash Operations (Redis-specific)

Work with Redis Hash data structures for storing object fields:

```csharp
// Set a field in a hash
await cacheService.HashSetAsync("user:123", "email", "user@example.com");

// Get a field from a hash
var email = await cacheService.HashGetAsync<string>("user:123", "email");

// Get all fields
var fields = await cacheService.HashGetAllAsync("user:123");

// Delete a field
await cacheService.HashDeleteAsync("user:123", "email");
```

**Note:** Memory cache provider throws `NotSupportedException` for hash operations.

#### List Operations (Redis-specific)

Work with Redis List data structures for ordered collections:

```csharp
// Push a value to the list
long length = await cacheService.ListPushAsync("queue:tasks", task);

// Pop a value from the list
var task = await cacheService.ListPopAsync<Task>("queue:tasks");

// Get a range of values
var tasks = await cacheService.ListRangeAsync<Task>("queue:tasks", 0, 9);

// Get list length
long count = await cacheService.ListLengthAsync("queue:tasks");
```

**Note:** Memory cache provider throws `NotSupportedException` for list operations.

#### Set Operations (Redis-specific)

Work with Redis Set data structures for unique values:

```csharp
// Add a value to a set
bool added = await cacheService.SetAddAsync("tags", "csharp");

// Remove a value from a set
bool removed = await cacheService.SetRemoveAsync("tags", "csharp");

// Get all set members
var tags = await cacheService.SetMembersAsync<string>("tags");

// Check if value is in set
bool contains = await cacheService.SetContainsAsync("tags", "csharp");
```

**Note:** Memory cache provider throws `NotSupportedException` for set operations.

#### Sorted Set Operations (Redis-specific)

Work with Redis Sorted Set data structures for scored values:

```csharp
// Add a value with score
bool added = await cacheService.SortedSetAddAsync("leaderboard", "player1", 1000.0);

// Remove a value
bool removed = await cacheService.SortedSetRemoveAsync("leaderboard", "player1");

// Get range by rank (ascending)
var topPlayers = await cacheService.SortedSetRangeAsync<string>(
    "leaderboard", 0, 9, SortOrder.Descending);

// Get score for a value
double? score = await cacheService.SortedSetScoreAsync("leaderboard", "player1");
```

**Note:** Memory cache provider throws `NotSupportedException` for sorted set operations.

### IDistributedLock

Provides distributed locking capability for coordinating access to shared resources across multiple processes or servers. Only available with Redis provider.

```csharp
// Acquire a lock
var lockToken = await distributedLock.AcquireLockAsync(
    key: "resource:123",
    timeout: TimeSpan.FromSeconds(5),
    expiration: TimeSpan.FromSeconds(30));

if (lockToken != null)
{
    try
    {
        // Perform critical section work
        await ProcessResource();
    }
    finally
    {
        // Always release the lock
        await distributedLock.ReleaseLockAsync(lockToken);
    }
}
else
{
    // Lock could not be acquired within timeout
    throw new InvalidOperationException("Could not acquire lock");
}
```

**Key Features:**
- Atomic lock acquisition using Redis SET NX EX
- Automatic retry with backoff until timeout
- Token-based verification prevents accidental release by other processes
- Configurable lock expiration prevents deadlocks

## Models

### CacheEntryOptions

Configures cache entry expiration strategies:

```csharp
public class CacheEntryOptions
{
    // Expire at a specific point in time
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    // Expire after a duration from when it's set
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    // Expire if not accessed within this duration (resets on each access)
    public TimeSpan? SlidingExpiration { get; set; }
}
```

#### Expiration Strategies

**Absolute Expiration:**
```csharp
// Expire at a specific time
await cacheService.SetAsync("key", value, new CacheEntryOptions
{
    AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
});

// Expire after a duration
await cacheService.SetAsync("key", value, new CacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
});
```

**Sliding Expiration:**
```csharp
// Expire if not accessed for 15 minutes
await cacheService.SetAsync("key", value, new CacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromMinutes(15)
});
```

**Priority:** If multiple expiration options are set, they are evaluated in this order:
1. AbsoluteExpiration
2. AbsoluteExpirationRelativeToNow
3. SlidingExpiration

### LockToken

Represents a distributed lock token returned by `AcquireLockAsync`:

```csharp
public sealed class LockToken
{
    public string Key { get; }           // The lock key
    public string Token { get; }         // Unique token for verification
    public DateTimeOffset AcquiredAt { get; } // When lock was acquired
}
```

The token must be provided to `ReleaseLockAsync` to release the lock. This prevents accidental release by other processes.

## Exceptions

The caching system defines custom exceptions for different error scenarios:

### CacheConnectionException

Thrown when a cache connection fails (typically Redis connection issues):

```csharp
try
{
    await cacheService.GetAsync<User>("user:123");
}
catch (CacheConnectionException ex)
{
    // Handle connection failure
    logger.LogError(ex, "Cache connection failed");
}
```

### CacheSerializationException

Thrown when serialization or deserialization fails:

```csharp
try
{
    await cacheService.SetAsync("key", complexObject);
}
catch (CacheSerializationException ex)
{
    // Handle serialization failure
    logger.LogError(ex, "Failed to serialize object");
}
```

### CacheTimeoutException

Thrown when a cache operation times out:

```csharp
public class CacheTimeoutException : Exception
{
    public string OperationName { get; }  // Name of the operation that timed out
    public TimeSpan Timeout { get; }      // Timeout duration
}
```

### CacheConfigurationException

Thrown when cache configuration is invalid:

```csharp
public class CacheConfigurationException : Exception
{
    public string SettingName { get; }  // Name of the invalid setting
}
```

## Usage Examples

### Basic Cache Operations

```csharp
public class UserService
{
    private readonly ICacheService _cache;

    public UserService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        // Try to get from cache first
        var cacheKey = $"user:{userId}";
        var user = await _cache.GetAsync<User>(cacheKey);

        if (user == null)
        {
            // Not in cache, load from database
            user = await LoadUserFromDatabaseAsync(userId);

            if (user != null)
            {
                // Store in cache for 30 minutes
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

### Distributed Locking

```csharp
public class OrderService
{
    private readonly IDistributedLock _lock;

    public OrderService(IDistributedLock distributedLock)
    {
        _lock = distributedLock;
    }

    public async Task ProcessOrderAsync(string orderId)
    {
        var lockKey = $"order:lock:{orderId}";
        var lockToken = await _lock.AcquireLockAsync(
            lockKey,
            timeout: TimeSpan.FromSeconds(5),
            expiration: TimeSpan.FromSeconds(30));

        if (lockToken == null)
        {
            throw new InvalidOperationException(
                $"Could not acquire lock for order {orderId}");
        }

        try
        {
            // Process order with exclusive access
            await ProcessOrderInternalAsync(orderId);
        }
        finally
        {
            await _lock.ReleaseLockAsync(lockToken);
        }
    }
}
```

### Error Handling

```csharp
public class CacheService
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheService> _logger;

    public async Task<T?> GetWithFallbackAsync<T>(string key, Func<Task<T>> fallback)
    {
        try
        {
            var value = await _cache.GetAsync<T>(key);
            if (value != null)
            {
                return value;
            }
        }
        catch (CacheConnectionException ex)
        {
            _logger.LogWarning(ex, "Cache connection failed, using fallback");
        }
        catch (CacheSerializationException ex)
        {
            _logger.LogError(ex, "Cache deserialization failed, using fallback");
        }
        catch (CacheTimeoutException ex)
        {
            _logger.LogWarning(ex, "Cache operation timed out, using fallback");
        }

        // Fallback to source
        return await fallback();
    }
}
```

## Provider Implementations

To use the caching system, install one of the provider packages:

### In-Memory Cache

```bash
dotnet add package MassLab.Common.Caching.Memory
```

See [MassLab.Common.Caching.Memory README](../MassLab.Common.Caching.Memory/README.md) for configuration and usage.

### Redis Cache

```bash
dotnet add package MassLab.Common.Caching.Redis
```

See [MassLab.Common.Caching.Redis README](../MassLab.Common.Caching.Redis/README.md) for configuration and usage.

## Design Principles

1. **Provider Agnostic**: Write code against `ICacheService` without coupling to specific implementations
2. **Strongly Typed**: Generic type parameters ensure type safety
3. **Async First**: All operations are asynchronous for non-blocking I/O
4. **Expiration Strategies**: Support both absolute and sliding expiration
5. **Error Handling**: Custom exceptions provide clear error context
6. **Redis Features**: Advanced Redis data structures available when needed
7. **Distributed Coordination**: Distributed locking for multi-instance scenarios

## Target Framework

- .NET 10.0

## Dependencies

- Microsoft.Extensions.DependencyInjection.Abstractions

## License

Copyright © MassLab.Common
