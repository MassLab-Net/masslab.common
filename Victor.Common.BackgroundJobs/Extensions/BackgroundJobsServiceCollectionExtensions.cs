using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Victor.Common.BackgroundJobs;
using Victor.Common.BackgroundJobs.Abstractions;

namespace Victor.Common.BackgroundJobs.Extensions;

/// <summary>
/// Service-collection helpers to register user-defined background-job classes.
/// </summary>
public static class BackgroundJobsServiceCollectionExtensions
{
    /// <summary>
    /// Registers a user job class implementing <see cref="IBackgroundJob{TPayload}"/>
    /// with scoped lifetime so providers (Hangfire / Quartz) can resolve it
    /// from DI at execution time.
    /// </summary>
    public static IServiceCollection AddBackgroundJob<TJob, TPayload>(this IServiceCollection services)
        where TJob : class, IBackgroundJob<TPayload>
    {
        services.AddScoped<TJob>();
        services.AddScoped<IBackgroundJob<TPayload>>(sp => sp.GetRequiredService<TJob>());
        return services;
    }

    /// <summary>
    /// Registers a recurring job bootstrapper that will run once at host startup
    /// when the selected provider wires <see cref="RecurringJobBootstrapperHostedService"/>.
    /// </summary>
    public static IServiceCollection AddRecurringJobBootstrapper<TBootstrapper>(this IServiceCollection services)
        where TBootstrapper : class, IRecurringJobBootstrapper
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecurringJobBootstrapper, TBootstrapper>());
        return services;
    }

    /// <summary>
    /// Registers the startup service that invokes all recurring-job bootstrappers.
    /// Providers call this after registering their <see cref="IBackgroundJobScheduler"/>.
    /// </summary>
    public static IServiceCollection AddRecurringJobBootstrappers(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RecurringJobBootstrapperHostedService>());
        return services;
    }
}
