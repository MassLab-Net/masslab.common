using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MassLab.Common.RateLimiting.Configuration;
using MassLab.Common.RateLimiting.Extensions;

namespace MassLab.Common.RateLimiting.Tests;

public class RateLimitingTests
{
    [Fact]
    public void AddMassLabRateLimiting_rejects_invalid_global_options()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "0"
            })
            .Build();

        var act = () => services.AddMassLabRateLimiting(configuration);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(RateLimitingOptions.PermitLimit));
    }

    [Fact]
    public void AddMassLabRateLimiting_rejects_invalid_policy_partition()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Policies:orders:PartitionBy"] = "tenant"
            })
            .Build();

        var act = () => services.AddMassLabRateLimiting(configuration);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(RateLimitPolicyOptions.PartitionBy));
    }

    [Fact]
    public void AddMassLabRateLimiting_registers_global_limiter()
    {
        var services = new ServiceCollection();

        services.AddMassLabRateLimiting();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        options.GlobalLimiter.Should().NotBeNull();
    }
}
