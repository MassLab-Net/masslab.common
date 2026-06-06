using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Victor.Common.BackgroundJobs.Abstractions;
using Victor.Common.BackgroundJobs.Configuration;
using Victor.Common.BackgroundJobs.Extensions;

namespace Victor.Common.BackgroundJobs.Hosted.Extensions;

/// <summary>
/// Registers an in-memory <see cref="IBackgroundJobScheduler"/> backed by a
/// hosted worker. Use only for process-local, non-durable jobs.
/// </summary>
public static class HostedBackgroundJobsServiceCollectionExtensions
{
    public static IServiceCollection AddVictorHostedBackgroundJobs(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = BackgroundJobsOptions.SectionName)
    {
        var options = new BackgroundJobsOptions();
        if (configuration != null)
        {
            services.Configure<BackgroundJobsOptions>(configuration.GetSection(sectionName));
            configuration.GetSection(sectionName).Bind(options);
        }
        else
        {
            services.Configure<BackgroundJobsOptions>(_ => { });
        }

        services.TryAddSingleton(Channel.CreateUnbounded<IHostedBackgroundJobInvocation>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
        }));

        services.TryAddSingleton<IBackgroundJobScheduler, HostedBackgroundJobScheduler>();
        services.AddRecurringJobBootstrappers();

        if (options.RunWorker)
            services.AddHostedService<HostedBackgroundJobWorker>();

        return services;
    }
}
