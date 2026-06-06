using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;
using MassLab.Common.Caching.Memory.Extensions;
using MassLab.Common.Caching.Redis.Extensions;

namespace MassLab.Common.Caching.Tests;

/// <summary>
/// Demonstrates how to switch between Memory and Redis cache providers
/// without changing application code. This shows the power of the ICacheService abstraction.
/// </summary>
public class ProviderSwitchingTests
{
    [Fact]
    public async Task SameCode_WorksWithMemoryProvider()
    {
        // Arrange - Configure with Memory cache
        var services = new ServiceCollection();
        services.AddMassLabMemoryCache(options =>
        {
            options.SizeLimit = 1000;
        });

        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<ICacheService>();

        // Act - Use the cache abstraction
        await ExecuteCacheOperations(cache);

        // Assert - Operations completed successfully
        var result = await cache.GetAsync<string>("test:key");
        result.Should().Be("test value");
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task SameCode_WorksWithRedisProvider()
    {
        // Arrange - Start Redis container (requires Docker)
        await using var redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await redisContainer.StartAsync();

        // Arrange - Configure with Redis cache
        var services = new ServiceCollection();
        services.AddMassLabRedisCache(options =>
        {
            options.ConnectionString = redisContainer.GetConnectionString();
            options.InstanceName = "test";
            options.DefaultExpiration = TimeSpan.FromMinutes(5);
        });

        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<ICacheService>();

        // Act - Use the SAME cache abstraction code
        await ExecuteCacheOperations(cache);

        // Assert - Operations completed successfully
        var result = await cache.GetAsync<string>("test:key");
        result.Should().Be("test value");
    }

    [Fact]
    public async Task ConfigurationBased_ProviderSelection()
    {
        // This test demonstrates how you can select providers based on configuration
        // In a real application, you would read from appsettings.json

        var useRedis = false; // This would come from configuration

        IServiceProvider serviceProvider;

        if (useRedis)
        {
            // Production configuration with Redis
            var services = new ServiceCollection();
            // In real app: services.AddMassLabRedisCache(configuration.GetSection("Redis"));
            serviceProvider = services.BuildServiceProvider();
        }
        else
        {
            // Development configuration with Memory cache
            var services = new ServiceCollection();
            services.AddMassLabMemoryCache(options =>
            {
            });
            serviceProvider = services.BuildServiceProvider();
        }

        // Application code remains the same regardless of provider
        var cache = serviceProvider.GetRequiredService<ICacheService>();
        
        await cache.SetAsync("config:test", "value");
        var result = await cache.GetAsync<string>("config:test");
        
        result.Should().Be("value");
    }

    [Fact]
    public async Task MemoryProvider_DoesNotRegisterAdvancedCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMassLabMemoryCache();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - IAdvancedCacheService should not be registered for memory provider
        var advanced = serviceProvider.GetService<IAdvancedCacheService>();
        advanced.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task RedisProvider_SupportsAllOperations()
    {
        // Arrange
        await using var redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await redisContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddMassLabRedisCache(options =>
        {
            options.ConnectionString = redisContainer.GetConnectionString();
            options.InstanceName = "test";
        });

        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IAdvancedCacheService>();

        // Act & Assert - All operations should work
        await cache.HashSetAsync("hash", "field", "value");
        var hashValue = await cache.HashGetAsync<string>("hash", "field");
        hashValue.Should().Be("value");

        await cache.ListPushAsync("list", "item");
        var listLength = await cache.ListLengthAsync("list");
        listLength.Should().Be(1);

        await cache.SetAddAsync("set", "member");
        var setContains = await cache.SetContainsAsync("set", "member");
        setContains.Should().BeTrue();

        await cache.SortedSetAddAsync("zset", "member", 1.0);
        var score = await cache.SortedSetScoreAsync("zset", "member");
        score.Should().Be(1.0);

        await cache.SetBinaryAsync("binary", new byte[] { 1, 2, 3 });
        var binary = await cache.GetBinaryAsync("binary");
        binary.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
    }

    /// <summary>
    /// Common cache operations that work with any provider.
    /// This demonstrates writing provider-agnostic code.
    /// </summary>
    private static async Task ExecuteCacheOperations(ICacheService cache)
    {
        // Basic operations that work with both providers
        await cache.SetAsync("test:key", "test value");
        
        var exists = await cache.ExistsAsync("test:key");
        exists.Should().BeTrue();

        var value = await cache.GetAsync<string>("test:key");
        value.Should().Be("test value");

        // Complex object storage
        var user = new TestUser 
        { 
            Id = 1, 
            Name = "Test User", 
            Email = "test@example.com" 
        };
        
        await cache.SetAsync("test:user:1", user);
        var retrievedUser = await cache.GetAsync<TestUser>("test:user:1");
        
        retrievedUser.Should().NotBeNull();
        retrievedUser!.Id.Should().Be(user.Id);
        retrievedUser.Name.Should().Be(user.Name);

        // Expiration
        var options = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        
        await cache.SetAsync("test:expiring", "value", options);
    }

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
