using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Victor.Common.RateLimiting.Configuration;
using Victor.Common.RateLimiting.Extensions;

namespace Victor.Common.RateLimiting.Tests;

public class RateLimitingTests
{
    [Fact]
    public void AddVictorRateLimiting_rejects_invalid_global_options()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "0"
            })
            .Build();

        var act = () => services.AddVictorRateLimiting(configuration);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(RateLimitingOptions.PermitLimit));
    }

    [Fact]
    public void AddVictorRateLimiting_rejects_invalid_policy_partition()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Policies:orders:PartitionBy"] = "tenant"
            })
            .Build();

        var act = () => services.AddVictorRateLimiting(configuration);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(RateLimitPolicyOptions.PartitionBy));
    }

    [Fact]
    public void AddVictorRateLimiting_registers_global_limiter()
    {
        var services = new ServiceCollection();

        services.AddVictorRateLimiting();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        options.GlobalLimiter.Should().NotBeNull();
    }
}
