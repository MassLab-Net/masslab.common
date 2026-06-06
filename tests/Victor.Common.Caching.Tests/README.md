# Victor.Common.Caching.Tests

Integration tests and examples for the Victor.Common.Caching library ecosystem.

## Overview

This test project demonstrates how to use the caching libraries with real Redis instances using Testcontainers. It includes comprehensive examples of:

- Redis cache operations with Testcontainers
- Provider switching between Memory and Redis
- Distributed locking across multiple connections
- All Redis data structures (Hash, List, Set, Sorted Set)
- Binary data operations
- Global vs scoped keys

## Prerequisites

- .NET 10.0 SDK
- Docker Desktop (for Testcontainers)

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~RedisIntegrationTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Test Structure

### RedisIntegrationTests

Comprehensive integration tests demonstrating Redis cache usage with Testcontainers.

**Key Tests:**
- `SetAsync_ThenGetAsync_ReturnsStoredValue` - Basic cache round-trip
- `SetAsync_WithExpiration_ExpiresAfterTimeout` - Expiration behavior
- `HashOperations_WorkCorrectly` - Redis Hash operations
- `ListOperations_WorkCorrectly` - Redis List operations
- `SetOperations_WorkCorrectly` - Redis Set operations
- `SortedSetOperations_WorkCorrectly` - Redis Sorted Set operations
- `BinaryOperations_WorkCorrectly` - Binary data storage
- `DistributedLock_AcquireAndRelease_WorksCorrectly` - Distributed locking
- `DistributedLock_MultipleConnections_PreventsRaceConditions` - Lock contention
- `GlobalKeys_SharedAcrossInstances` - Global key sharing

### ProviderSwitchingTests

Demonstrates how to write provider-agnostic code that works with both Memory and Redis providers.

**Key Tests:**
- `SameCode_WorksWithMemoryProvider` - Same code with Memory cache
- `SameCode_WorksWithRedisProvider` - Same code with Redis cache
- `ConfigurationBased_ProviderSelection` - Configuration-based provider selection
- `MemoryProvider_ThrowsForRedisSpecificOperations` - Memory provider limitations
- `RedisProvider_SupportsAllOperations` - Redis provider capabilities

## Testcontainers Setup

The tests use Testcontainers to automatically start and stop Redis containers:

```csharp
// Create and start a Redis container
_redisContainer = new RedisBuilder()
    .WithImage("redis:7-alpine")
    .Build();

await _redisContainer.StartAsync();

// Configure services with the test container connection string
var services = new ServiceCollection();
services.AddVictorRedisCache(options =>
{
    options.ConnectionString = _redisContainer.GetConnectionString();
    options.InstanceName = "test";
});
```

## Provider Switching Example

The same application code works with both providers:

```csharp
// Development - Use Memory cache
services.AddVictorMemoryCache(options =>
{
    options.SizeLimit = 1000;
});

// Production - Use Redis cache
services.AddVictorRedisCache(options =>
{
    options.ConnectionString = "localhost:6379";
    options.InstanceName = "myapp";
});

// Application code remains the same
var cache = serviceProvider.GetRequiredService<ICacheService>();
await cache.SetAsync("key", "value");
var result = await cache.GetAsync<string>("key");
```

## Distributed Locking Example

```csharp
var lockService = serviceProvider.GetRequiredService<IDistributedLock>();

var token = await lockService.AcquireLockAsync(
    key: "resource:lock",
    timeout: TimeSpan.FromSeconds(5),
    expiration: TimeSpan.FromSeconds(10)
);

if (token != null)
{
    try
    {
        // Critical section - only one instance can execute this
        await DoWork();
    }
    finally
    {
        await lockService.ReleaseLockAsync(token);
    }
}
```

## Redis Data Structures Example

```csharp
// Hash - Store object fields
await cache.HashSetAsync("user:123", "name", "John Doe");
await cache.HashSetAsync("user:123", "email", "john@example.com");
var name = await cache.HashGetAsync<string>("user:123", "name");

// List - Ordered collection
await cache.ListPushAsync("queue:tasks", "task1");
await cache.ListPushAsync("queue:tasks", "task2");
var task = await cache.ListPopAsync<string>("queue:tasks");

// Set - Unique values
await cache.SetAddAsync("tags", "important");
await cache.SetAddAsync("tags", "urgent");
var members = await cache.SetMembersAsync<string>("tags");

// Sorted Set - Scored values
await cache.SortedSetAddAsync("leaderboard", "player1", 100);
await cache.SortedSetAddAsync("leaderboard", "player2", 200);
var topPlayers = await cache.SortedSetRangeAsync<string>(
    "leaderboard", 
    order: SortOrder.Descending
);
```

## Notes

- Tests use `IAsyncLifetime` to manage container lifecycle
- Each test class creates its own Redis container for isolation
- Containers are automatically cleaned up after tests complete
- The tests demonstrate real-world usage patterns
- All tests are async and use FluentAssertions for readable assertions

## Troubleshooting

**Docker not running:**
```
Error: Docker is not running
Solution: Start Docker Desktop
```

**Port conflicts:**
```
Error: Port already in use
Solution: Testcontainers automatically assigns random ports
```

**Slow test execution:**
```
Issue: Container startup takes time
Solution: This is normal for the first run; subsequent runs are faster
```
