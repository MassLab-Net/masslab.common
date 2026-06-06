using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;
using Victor.Common.BackgroundJobs.Abstractions;
using Victor.Common.BackgroundJobs.Configuration;
using Victor.Common.BackgroundJobs.Extensions;

namespace Victor.Common.BackgroundJobs.Quartz.Extensions;

/// <summary>
/// Service-collection extensions to register Quartz.NET as the Victor
/// <see cref="IBackgroundJobScheduler"/> implementation.
/// </summary>
public static class QuartzServiceCollectionExtensions
{
    /// <summary>
    /// Registers Quartz with RAM job-store and scheduler-factory + the
    /// background-job scheduler bridge.
    /// </summary>
    public static IServiceCollection AddVictorQuartz(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<IServiceCollectionQuartzConfigurator>? configureQuartz = null,
        string sectionName = BackgroundJobsOptions.SectionName)
    {
        var jobOptions = new BackgroundJobsOptions();
        if (configuration != null)
        {
            services.Configure<BackgroundJobsOptions>(configuration.GetSection(sectionName));
            configuration.GetSection(sectionName).Bind(jobOptions);
        }
        else
        {
            services.Configure<BackgroundJobsOptions>(_ => { });
        }

        services.AddQuartz(q =>
        {
            // RAM job store by default; UseInMemoryStore is implicit.
            // Adapters are resolved via DI through Microsoft.Extensions.DI.
            configureQuartz?.Invoke(q);
        });

        if (jobOptions.RunWorker)
        {
            services.AddQuartzHostedService(o =>
            {
                o.WaitForJobsToComplete = true;
            });
        }

        services.TryAddSingleton<IBackgroundJobScheduler, QuartzBackgroundJobScheduler>();
        services.AddRecurringJobBootstrappers();

        return services;
    }
}
