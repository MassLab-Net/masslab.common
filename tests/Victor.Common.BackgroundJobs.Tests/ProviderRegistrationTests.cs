using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Victor.Common.BackgroundJobs.Hangfire.Extensions;
using Victor.Common.BackgroundJobs.Quartz.Extensions;

namespace Victor.Common.BackgroundJobs.Tests;

public class ProviderRegistrationTests
{
    [Fact]
    public void Hangfire_provider_respects_run_worker_false()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVictorHangfire(ConfigurationWithWorkerDisabled());

        services.Any(IsHangfireHostedService).Should().BeFalse();
    }

    [Fact]
    public void Quartz_provider_respects_run_worker_false()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVictorQuartz(ConfigurationWithWorkerDisabled());

        services.Any(IsQuartzHostedService).Should().BeFalse();
    }

    private static IConfiguration ConfigurationWithWorkerDisabled() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundJobs:RunWorker"] = "false"
            })
            .Build();

    private static bool IsHangfireHostedService(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(IHostedService)
        && descriptor.ImplementationType is { FullName: { } fullName }
        && fullName.Contains("HangfireServerHostedService", StringComparison.Ordinal);

    private static bool IsQuartzHostedService(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(IHostedService)
        && descriptor.ImplementationType is { FullName: { } fullName }
        && fullName.Contains("QuartzHostedService", StringComparison.Ordinal);
}
