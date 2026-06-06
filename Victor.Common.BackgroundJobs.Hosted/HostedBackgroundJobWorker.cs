using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Victor.Common.BackgroundJobs.Configuration;

namespace Victor.Common.BackgroundJobs.Hosted;

/// <summary>
/// Consumes in-memory hosted background jobs.
/// </summary>
public sealed class HostedBackgroundJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<IHostedBackgroundJobInvocation> _queue;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<HostedBackgroundJobWorker> _logger;

    public HostedBackgroundJobWorker(
        IServiceProvider serviceProvider,
        Channel<IHostedBackgroundJobInvocation> queue,
        IOptions<BackgroundJobsOptions> options,
        ILogger<HostedBackgroundJobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerCount = Math.Max(1, _options.WorkerCount);
        var workers = Enumerable.Range(0, workerCount)
            .Select(index => ConsumeAsync(index, stoppingToken))
            .ToArray();

        await Task.WhenAll(workers).ConfigureAwait(false);
    }

    private async Task ConsumeAsync(int workerIndex, CancellationToken cancellationToken)
    {
        await foreach (var invocation in _queue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await invocation.ExecuteAsync(_serviceProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hosted background job {JobId} failed on worker {WorkerIndex}",
                    invocation.Id,
                    workerIndex);
            }
        }
    }
}
