using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MassLab.Common.BackgroundJobs.Abstractions;
using MassLab.Common.BackgroundJobs.Configuration;
using MassLab.Common.BackgroundJobs.Extensions;

namespace MassLab.Common.BackgroundJobs.Hosted.Extensions;

/// <summary>
/// Registers an in-memory <see cref="IBackgroundJobScheduler"/> backed by a
/// hosted worker. Use only for process-local, non-durable jobs.
/// </summary>
public static class HostedBackgroundJobsServiceCollectionExtensions
{
    public static IServiceCollection AddMassLabHostedBackgroundJobs(
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
