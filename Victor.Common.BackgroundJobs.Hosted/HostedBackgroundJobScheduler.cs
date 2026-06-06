using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Victor.Common.BackgroundJobs.Abstractions;

namespace Victor.Common.BackgroundJobs.Hosted;

/// <summary>
/// In-memory scheduler backed by <see cref="HostedBackgroundJobWorker"/>.
/// Jobs are not persisted and are lost when the process exits.
/// </summary>
public sealed class HostedBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly Channel<IHostedBackgroundJobInvocation> _queue;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _scheduledJobs = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _recurringJobs = new();
    private readonly ILogger<HostedBackgroundJobScheduler> _logger;

    public HostedBackgroundJobScheduler(
        Channel<IHostedBackgroundJobInvocation> queue,
        ILogger<HostedBackgroundJobScheduler> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public async Task<string> EnqueueAsync<TJob, TPayload>(
        TPayload payload,
        CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        var id = NewId();
        await _queue.Writer.WriteAsync(new HostedBackgroundJobInvocation<TJob, TPayload>(id, payload), cancellationToken)
            .ConfigureAwait(false);
        _logger.LogDebug("Hosted enqueue {Job} id={Id}", typeof(TJob).Name, id);
        return id;
    }

    public Task<string> ScheduleAsync<TJob, TPayload>(
        TPayload payload,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
        => ScheduleAsync<TJob, TPayload>(payload, DateTimeOffset.UtcNow.Add(delay), cancellationToken);

    public Task<string> ScheduleAsync<TJob, TPayload>(
        TPayload payload,
        DateTimeOffset runAt,
        CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        var id = NewId();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _scheduledJobs[id] = cts;
        _ = RunScheduledAsync(id, new HostedBackgroundJobInvocation<TJob, TPayload>(id, payload), runAt, cts);
        _logger.LogDebug("Hosted schedule {Job} id={Id} runAt={RunAt}", typeof(TJob).Name, id, runAt);
        return Task.FromResult(id);
    }

    public Task AddOrUpdateRecurringAsync<TJob, TPayload>(
        string jobId,
        TPayload payload,
        string cronExpression,
        CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job id is required.", nameof(jobId));

        var schedule = SimpleCronSchedule.Parse(cronExpression);
        _ = RemoveRecurringAsync(jobId, cancellationToken);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _recurringJobs[jobId] = cts;
        _ = RunRecurringAsync(jobId, () => new HostedBackgroundJobInvocation<TJob, TPayload>(jobId, payload), schedule, cts);

        _logger.LogDebug("Hosted recurring {Job} id={Id} cron={Cron}", typeof(TJob).Name, jobId, cronExpression);
        return Task.CompletedTask;
    }

    public Task RemoveRecurringAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (_recurringJobs.TryRemove(jobId, out var cts))
        {
            cts.Cancel();
        }

        return Task.CompletedTask;
    }

    public Task CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (_scheduledJobs.TryRemove(jobId, out var scheduledCts))
        {
            scheduledCts.Cancel();
        }

        if (_recurringJobs.TryRemove(jobId, out var recurringCts))
        {
            recurringCts.Cancel();
        }

        return Task.CompletedTask;
    }

    private async Task RunScheduledAsync(
        string id,
        IHostedBackgroundJobInvocation invocation,
        DateTimeOffset runAt,
        CancellationTokenSource cts)
    {
        try
        {
            var delay = runAt - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cts.Token).ConfigureAwait(false);

            await _queue.Writer.WriteAsync(invocation, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Hosted scheduled job {Id} cancelled", id);
        }
        finally
        {
            TryRemoveSame(_scheduledJobs, id, cts);
            cts.Dispose();
        }
    }

    private async Task RunRecurringAsync(
        string jobId,
        Func<IHostedBackgroundJobInvocation> invocationFactory,
        SimpleCronSchedule schedule,
        CancellationTokenSource cts)
    {
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var next = schedule.GetNextOccurrence(DateTimeOffset.UtcNow);
                var delay = next - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cts.Token).ConfigureAwait(false);

                await _queue.Writer.WriteAsync(invocationFactory(), cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Hosted recurring job {Id} cancelled", jobId);
        }
        finally
        {
            TryRemoveSame(_recurringJobs, jobId, cts);
            cts.Dispose();
        }
    }

    private static string NewId() => Guid.NewGuid().ToString("N");

    private static void TryRemoveSame(
        ConcurrentDictionary<string, CancellationTokenSource> jobs,
        string jobId,
        CancellationTokenSource cts)
        => ((ICollection<KeyValuePair<string, CancellationTokenSource>>)jobs)
            .Remove(new KeyValuePair<string, CancellationTokenSource>(jobId, cts));
}
