using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using MassLab.Common.Caching.Exceptions;
using MassLab.Common.Caching.Redis;
using MassLab.Common.Caching.Redis.Configuration;
using MassLab.Common.Caching.Redis.Extensions;
using MassLab.Common.Caching.Redis.Serialization;

namespace MassLab.Common.Caching.Tests;

public class RedisRegistrationTests
{
    [Fact]
    public void AddMassLabRedisCache_WithConfiguration_RegistersRedisServicesWithoutConnecting()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "localhost:6379",
                ["InstanceName"] = "unit"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddMassLabRedisCache(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(Lazy<IConnectionMultiplexer>));
        services.Should().Contain(d => d.ServiceType == typeof(IConnectionMultiplexer));
        services.Should().Contain(d => d.ServiceType == typeof(ICacheSerializer)
            && d.ImplementationType == typeof(JsonCacheSerializer));
        services.Should().Contain(d => d.ServiceType == typeof(ICacheService)
            && d.ImplementationType == typeof(RedisCacheService));
        services.Should().Contain(d => d.ServiceType == typeof(IAdvancedCacheService));
        services.Should().Contain(d => d.ServiceType == typeof(IDistributedLock));
    }

    [Fact]
    public void AddMassLabRedisCache_WithMissingConnectionString_ThrowsConfigurationException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMassLabRedisCache(_ => { });

        act.Should().Throw<CacheConfigurationException>()
            .WithMessage("*ConnectionString*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddMassLabRedisCache_WithInvalidConnectTimeout_ThrowsConfigurationException(int seconds)
    {
        var services = new ServiceCollection();

        var act = () => services.AddMassLabRedisCache(options =>
        {
            options.ConnectionString = "localhost:6379";
            options.ConnectTimeout = TimeSpan.FromSeconds(seconds);
        });

        act.Should().Throw<CacheConfigurationException>()
            .WithMessage("*ConnectTimeout*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddMassLabRedisCache_WithInvalidOperationTimeout_ThrowsConfigurationException(int seconds)
    {
        var services = new ServiceCollection();

        var act = () => services.AddMassLabRedisCache(options =>
        {
            options.ConnectionString = "localhost:6379";
            options.OperationTimeout = TimeSpan.FromSeconds(seconds);
        });

        act.Should().Throw<CacheConfigurationException>()
            .WithMessage("*OperationTimeout*");
    }
}
