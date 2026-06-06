using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.Redis;
using Victor.Common.Caching.Redis;
using Victor.Common.Caching.Redis.Configuration;
using Victor.Common.Caching.Redis.Extensions;

namespace Victor.Common.Caching.Tests;

/// <summary>
/// Integration tests demonstrating Redis cache usage with Testcontainers.
/// These tests show how to set up Redis in a containerized environment for testing.
/// </summary>
/// <remarks>
/// Requires a running Docker daemon. Filter out with
/// <c>dotnet test --filter "Category!=Docker"</c> when Docker is unavailable.
/// </remarks>
[Trait("Category", "Docker")]
public class RedisIntegrationTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initialize the Redis container before tests run.
    /// </summary>
    public async Task InitializeAsync()
    {
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
            options.DefaultExpiration = TimeSpan.FromMinutes(5);
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Clean up the Redis container after tests complete.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (_redisContainer != null)
        {
            await _redisContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        // Arrange
        var cache = _serviceProvider!.GetRequiredService<ICacheService>();
        var key = "test:user:123";
        var value = new TestUser { Id = 123, Name = "John Doe", Email = "john@example.com" };

        // Act
        await cache.SetAsync(key, value);
        var retrieved = await cache.GetAsync<TestUser>(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(value.Id);
        retrieved.Name.Should().Be(value.Name);
        retrieved.Email.Should().Be(value.Email);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ExpiresAfterTimeout()
    {
        // Arrange
        var cache = _serviceProvider!.GetRequiredService<ICacheService>();
        var key = "test:expiring:key";
        var value = "temporary value";
        var options = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
        };

        // Act
        await cache.SetAsync(key, value, options);
        var immediateResult = await cache.GetAsync<string>(key);
        
        await Task.Delay(TimeSpan.FromSeconds(3));
        var expiredResult = await cache.GetAsync<string>(key);

        // Assert
        immediateResult.Should().Be(value);
        expiredResult.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_DeletesKey()
    {
        // Arrange
        var cache = _serviceProvider!.GetRequiredService<ICacheService>();
        var key = "test:delete:key";
        var value = "to be deleted";

        // Act
        await cache.SetAsync(key, value);
        var beforeDelete = await cache.ExistsAsync(key);
        
        await cache.RemoveAsync(key);
        var afterDelete = await cache.ExistsAsync(key);

        // Assert
        beforeDelete.Should().BeTrue();
        afterDelete.Should().BeFalse();
    }

    [Fact]
    public async Task HashOperations_WorkCorrectly()
    {
        // Arrange
        var cache = _serviceProvider!.GetRequiredService<IAdvancedCacheService>();
        var key = "test:user:profile";

        // Act - Set hash fields
        await cache.HashSetAsync(key, "name", "Jane Doe");
        await cache.HashSetAsync(key, "email", "jane@example.com");
        await cache.HashSetAsync(key, "age", 30);

        // Act - Get individual fields
        var name = await cache.HashGetAsync<string>(key, "name");
        var email = await cache.HashGetAsync<string>(key, "email");
        var age = await cache.HashGetAsync<int>(key, "age");

        // Act - Get all fields
        var allFields = await cache.HashGetAllAsync(key);

        // Assert
        name.Should().Be("Jane Doe");
        email.Should().Be("jane@example.com");
        age.Should().Be(30);
        allFields.Should().HaveCount(3);
        allFields.Should().ContainKey("name");
        allFields.Should().ContainKey("email");
        allFields.Should().ContainKey("age");
    }

    [Fact]
    public async Task ListOperations_WorkCorrectly()
    {
        // Arrange
        var cache = _serviceProvider!.GetRequiredService<IAdvancedCacheService>();
        var key = "test:queue:tasks";

        // Act - Push items
        await cache.ListPushAsync(key, "task1");
        await cache.ListPushAsync(key, "task2");
        await cache.ListPushAsync(key, "task3");

        var length = await cache.ListLengthAsync(key);
        var range = await cache.ListRangeAsync<string>(key);
        var popped = await cache.ListPopAsync<string>(key);

        // Assert
        length.Should().Be(3);
        range.Should().HaveCount(3);
        range.Should().ContainInOrder("task3", "task2", "task1"); // LIFO order
        popped.Should().Be("task3");
    }

    [Fact]
    public async Task SetOperations_WorkCorrectly()
    {
        // Arrange
        var cache = _serviceProvider!.GetRequiredService<IAdvancedCacheService>();
        var key = "test:tags";

        // Act
        var added1 = await cache.SetAddAsync(key, "tag1");
        var added2 = await cache.SetAddAsync(key, "tag2");
        var addedDuplicate = await cache.SetAddAsync(key, "tag1");

        var members = await cache.SetMembersAsync<string>(key);
        var contains = await cache.SetContainsAsync(key, "tag1");
        var notContains = await cache.SetContainsAsync(key, "tag3");

        // Assert
        added1.Should().BeTrue();
        added2.Should().BeTrue();
        addedDuplicate.Should().BeFalse(); // Duplicate not added
        members.Should().HaveCount(2);
        contains.Should().BeTrue();
        notContains.Should().BeFalse();
    }

    [Fact]
    public async Task SortedSetOperations_WorkCorrectly()
    {
        // Arrange
        var cache = _serviceProvider!.GetRequiredService<IAdvancedCacheService>();
        var key = "test:leaderboard";

        // Act
        await cache.SortedSetAddAsync(key, "player1", 100);
        await cache.SortedSetAddAsync(key, "player2", 200);
        await cache.SortedSetAddAsync(key, "player3", 150);

        var ascending = await cache.SortedSetRangeAsync<string>(key, order: SortOrder.Ascending);
        var descending = await cache.SortedSetRangeAsync<string>(key, order: SortOrder.Descending);
        var score = await cache.SortedSetScoreAsync(key, "player2");

        // Assert
        ascending.Should().ContainInOrder("player1", "player3", "player2");
        descending.Should().ContainInOrder("player2", "player3", "player1");
        score.Should().Be(200);
    }

    [Fact]
    public async Task BinaryOperations_WorkCorrectly()
    {
        // Arrange
        var cache = _serviceProvider!.GetRequiredService<IAdvancedCacheService>();
        var key = "test:binary:data";
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

        // Act
        await cache.SetBinaryAsync(key, data);
        var retrieved = await cache.GetBinaryAsync(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task DistributedLock_AcquireAndRelease_WorksCorrectly()
    {
        // Arrange
        var lockService = _serviceProvider!.GetRequiredService<IDistributedLock>();
        var lockKey = "test:lock:resource";
        var timeout = TimeSpan.FromSeconds(5);
        var expiration = TimeSpan.FromSeconds(10);

        // Act
        var token = await lockService.AcquireLockAsync(lockKey, timeout, expiration);
        var released = await lockService.ReleaseLockAsync(token!);

        // Assert
        token.Should().NotBeNull();
        token!.Key.Should().Be(lockKey);
        token.Token.Should().NotBeNullOrEmpty();
        released.Should().BeTrue();
    }

    [Fact]
    public async Task DistributedLock_Contention_ReturnsNull()
    {
        // Arrange
        var lockService = _serviceProvider!.GetRequiredService<IDistributedLock>();
        var lockKey = "test:lock:contended";
        var timeout = TimeSpan.FromMilliseconds(100);
        var expiration = TimeSpan.FromSeconds(10);

        // Act
        var firstToken = await lockService.AcquireLockAsync(lockKey, timeout, expiration);
        var secondToken = await lockService.AcquireLockAsync(lockKey, timeout, expiration);

        // Assert
        firstToken.Should().NotBeNull();
        secondToken.Should().BeNull(); // Lock already held

        // Cleanup
        if (firstToken != null)
        {
            await lockService.ReleaseLockAsync(firstToken);
        }
    }

    [Fact]
    public async Task DistributedLock_MultipleConnections_PreventsRaceConditions()
    {
        // Arrange - Create two separate service providers (simulating different app instances)
        var services1 = new ServiceCollection();
        services1.AddVictorRedisCache(options =>
        {
            options.ConnectionString = _redisContainer!.GetConnectionString();
            options.InstanceName = "instance1";
        });
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddVictorRedisCache(options =>
        {
            options.ConnectionString = _redisContainer!.GetConnectionString();
            options.InstanceName = "instance2";
        });
        var provider2 = services2.BuildServiceProvider();

        var lock1 = provider1.GetRequiredService<IDistributedLock>();
        var lock2 = provider2.GetRequiredService<IDistributedLock>();

        var lockKey = "test:lock:shared";
        var timeout = TimeSpan.FromMilliseconds(100);
        var expiration = TimeSpan.FromSeconds(5);

        // Act
        var token1 = await lock1.AcquireLockAsync(lockKey, timeout, expiration);
        var token2 = await lock2.AcquireLockAsync(lockKey, timeout, expiration);

        // Assert
        token1.Should().NotBeNull("First instance should acquire lock");
        token2.Should().BeNull("Second instance should fail to acquire lock");

        // Cleanup
        if (token1 != null)
        {
            await lock1.ReleaseLockAsync(token1);
        }

        (provider1 as IDisposable)?.Dispose();
        (provider2 as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task GlobalKeys_SharedAcrossInstances()
    {
        // Arrange - Create two service providers with different instance names
        var services1 = new ServiceCollection();
        services1.AddVictorRedisCache(options =>
        {
            options.ConnectionString = _redisContainer!.GetConnectionString();
            options.InstanceName = "app1";
        });
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddVictorRedisCache(options =>
        {
            options.ConnectionString = _redisContainer!.GetConnectionString();
            options.InstanceName = "app2";
        });
        var provider2 = services2.BuildServiceProvider();

        var cache1 = provider1.GetRequiredService<ICacheService>();
        var cache2 = provider2.GetRequiredService<ICacheService>();

        var globalKey = "global:config:version";
        var value = "1.0.0";

        // Act
        await cache1.SetGlobalAsync(globalKey, value);
        var retrievedFromCache2 = await cache2.GetGlobalAsync<string>(globalKey);

        // Assert
        retrievedFromCache2.Should().Be(value, "Global keys should be shared across instances");

        // Cleanup
        (provider1 as IDisposable)?.Dispose();
        (provider2 as IDisposable)?.Dispose();
    }

    // Test model
    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
