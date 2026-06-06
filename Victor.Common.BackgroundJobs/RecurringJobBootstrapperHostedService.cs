using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Victor.Common.BackgroundJobs.Abstractions;

namespace Victor.Common.BackgroundJobs;

/// <summary>
/// Runs all registered recurring-job bootstrappers once during host startup.
/// </summary>
public sealed class RecurringJobBootstrapperHostedService : IHostedService
{
    private readonly IBackgroundJobScheduler _scheduler;
    private readonly IEnumerable<IRecurringJobBootstrapper> _bootstrappers;
    private readonly ILogger<RecurringJobBootstrapperHostedService> _logger;

    public RecurringJobBootstrapperHostedService(
        IBackgroundJobScheduler scheduler,
        IEnumerable<IRecurringJobBootstrapper> bootstrappers,
        ILogger<RecurringJobBootstrapperHostedService> logger)
    {
        _scheduler = scheduler;
        _bootstrappers = bootstrappers;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var bootstrapper in _bootstrappers)
        {
            _logger.LogInformation("Registering recurring jobs via {Bootstrapper}", bootstrapper.GetType().Name);
            await bootstrapper.RegisterAsync(_scheduler, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
