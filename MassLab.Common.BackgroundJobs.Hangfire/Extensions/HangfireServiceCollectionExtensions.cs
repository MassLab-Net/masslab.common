using Hangfire;
using Hangfire.Dashboard;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MassLab.Common.BackgroundJobs.Extensions;
using MassLab.Common.BackgroundJobs.Abstractions;
using MassLab.Common.BackgroundJobs.Configuration;

namespace MassLab.Common.BackgroundJobs.Hangfire.Extensions;

/// <summary>
/// Service-collection &amp; application-builder extensions to wire Hangfire as
/// the <see cref="IBackgroundJobScheduler"/> implementation.
/// </summary>
public static class HangfireServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hangfire (in-memory storage by default) and bridges
    /// <see cref="IBackgroundJobScheduler"/> to it.
    /// </summary>
    public static IServiceCollection AddMassLabHangfire(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<IGlobalConfiguration>? configureHangfire = null,
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

        services.AddHangfire(c =>
        {
            c.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
            c.UseSimpleAssemblyNameTypeSerializer();
            c.UseRecommendedSerializerSettings();
            // Default storage: in-memory. Override via configureHangfire callback.
            c.UseInMemoryStorage();
            configureHangfire?.Invoke(c);
        });

        if (jobOptions.RunWorker)
        {
            services.AddHangfireServer((sp, options) =>
            {
                var jobOpts = sp.GetRequiredService<IOptions<BackgroundJobsOptions>>().Value;
                options.WorkerCount = jobOpts.WorkerCount;
                options.Queues = new[] { jobOpts.QueueName };
            });
        }

        services.TryAddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
        services.AddRecurringJobBootstrappers();

        return services;
    }

    /// <summary>
    /// Mounts the Hangfire dashboard at <c>/hangfire</c> (or the supplied path).
    /// By default, requires the user to be authenticated. Pass custom
    /// <see cref="DashboardOptions"/> to override authorization.
    /// </summary>
    public static IApplicationBuilder UseHangfireDashboardSafe(
        this IApplicationBuilder app,
        string path = "/hangfire",
        DashboardOptions? options = null)
    {
        options ??= new DashboardOptions
        {
            Authorization = [new AuthenticatedDashboardFilter()]
        };
        app.UseHangfireDashboard(path, options);
        return app;
    }
}

/// <summary>
/// Default Hangfire dashboard authorization filter that requires an authenticated user.
/// </summary>
public class AuthenticatedDashboardFilter : global::Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    /// <inheritdoc />
    public bool Authorize(global::Hangfire.Dashboard.DashboardContext context)
    {
        // In Hangfire.AspNetCore, DashboardContext.GetHttpContext() is available
        // via the Microsoft.AspNetCore.Http feature.
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true;
    }
}
