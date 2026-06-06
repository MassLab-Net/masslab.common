using Hangfire;
using Microsoft.Extensions.Logging;
using Victor.Common.BackgroundJobs.Abstractions;

namespace Victor.Common.BackgroundJobs.Hangfire;

/// <summary>
/// Hangfire bridge: serializes the call to <c>IBackgroundJob&lt;TPayload&gt;.ExecuteAsync</c>
/// using the strongly-typed Hangfire client. The job runs inside a DI scope
/// that resolves <typeparamref name="TJob"/> at execution time.
/// </summary>
public class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IBackgroundJobClient _client;
    private readonly IRecurringJobManager _recurringJobs;
    private readonly ILogger<HangfireBackgroundJobScheduler> _logger;

    /// <summary>Initializes a new instance.</summary>
    public HangfireBackgroundJobScheduler(
        IBackgroundJobClient client,
        IRecurringJobManager recurringJobs,
        ILogger<HangfireBackgroundJobScheduler> logger)
    {
        _client = client;
        _recurringJobs = recurringJobs;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> EnqueueAsync<TJob, TPayload>(TPayload payload, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        var id = _client.Enqueue<TJob>(j => j.ExecuteAsync(payload, CancellationToken.None));
        _logger.LogDebug("Hangfire enqueue {Job} id={Id}", typeof(TJob).Name, id);
        return Task.FromResult(id);
    }

    /// <inheritdoc />
    public Task<string> ScheduleAsync<TJob, TPayload>(TPayload payload, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        var id = _client.Schedule<TJob>(j => j.ExecuteAsync(payload, CancellationToken.None), delay);
        _logger.LogDebug("Hangfire schedule {Job} id={Id} delay={Delay}", typeof(TJob).Name, id, delay);
        return Task.FromResult(id);
    }

    /// <inheritdoc />
    public Task<string> ScheduleAsync<TJob, TPayload>(TPayload payload, DateTimeOffset runAt, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        var id = _client.Schedule<TJob>(j => j.ExecuteAsync(payload, CancellationToken.None), runAt);
        _logger.LogDebug("Hangfire schedule {Job} id={Id} runAt={RunAt}", typeof(TJob).Name, id, runAt);
        return Task.FromResult(id);
    }

    /// <inheritdoc />
    public Task AddOrUpdateRecurringAsync<TJob, TPayload>(
        string jobId,
        TPayload payload,
        string cronExpression,
        CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        _recurringJobs.AddOrUpdate<TJob>(jobId, j => j.ExecuteAsync(payload, CancellationToken.None), cronExpression, new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc,
        });
        _logger.LogDebug("Hangfire recurring {Job} id={Id} cron={Cron}", typeof(TJob).Name, jobId, cronExpression);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveRecurringAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _recurringJobs.RemoveIfExists(jobId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        BackgroundJob.Delete(jobId);
        return Task.CompletedTask;
    }
}
