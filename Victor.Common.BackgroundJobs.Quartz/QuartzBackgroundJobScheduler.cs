using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Victor.Common.BackgroundJobs.Abstractions;

namespace Victor.Common.BackgroundJobs.Quartz;

/// <summary>
/// Quartz bridge for <see cref="IBackgroundJobScheduler"/>. The actual unit
/// of execution is <see cref="VictorJobAdapter{TJob,TPayload}"/>; this
/// scheduler builds the IJobDetail / ITrigger and submits to Quartz.
/// </summary>
public class QuartzBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly ISchedulerFactory _factory;
    private readonly ILogger<QuartzBackgroundJobScheduler> _logger;
    internal const string PayloadKey = "__victor_payload";
    internal const string PayloadTypeKey = "__victor_payload_type";
    internal const string RecurringGroup = "victor-recurring";

    /// <summary>Initializes a new instance.</summary>
    public QuartzBackgroundJobScheduler(ISchedulerFactory factory, ILogger<QuartzBackgroundJobScheduler> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    private static (IJobDetail Job, JobKey Key) BuildJob<TJob, TPayload>(
        TPayload payload,
        string? jobId = null)
        where TJob : IBackgroundJob<TPayload>
    {
        var isConcurrent = typeof(IConcurrentJob).IsAssignableFrom(typeof(TJob));
        var adapterType = isConcurrent
            ? typeof(ConcurrentVictorJobAdapter<,>).MakeGenericType(typeof(TJob), typeof(TPayload)!)
            : typeof(VictorJobAdapter<,>).MakeGenericType(typeof(TJob), typeof(TPayload)!);
        var group = jobId is not null ? RecurringGroup : (typeof(TJob).FullName ?? typeof(TJob).Name);
        var key = new JobKey(jobId ?? Guid.NewGuid().ToString("N"), group);
        var data = new JobDataMap
        {
            { PayloadKey, JsonSerializer.Serialize(payload) },
            { PayloadTypeKey, typeof(TPayload).AssemblyQualifiedName ?? typeof(TPayload).FullName ?? typeof(TPayload).Name },
        };
        var job = JobBuilder.Create(adapterType)
            .WithIdentity(key)
            .UsingJobData(data)
            .StoreDurably(false)
            .Build();
        return (job, key);
    }

    /// <inheritdoc />
    public async Task<string> EnqueueAsync<TJob, TPayload>(TPayload payload, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        var scheduler = await _factory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var (job, key) = BuildJob<TJob, TPayload>(payload);
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{key.Name}-trg", key.Group)
            .StartNow()
            .Build();
        await scheduler.ScheduleJob(job, trigger, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Quartz enqueue {Job} key={Key}", typeof(TJob).Name, key);
        return key.ToString();
    }

    /// <inheritdoc />
    public Task<string> ScheduleAsync<TJob, TPayload>(TPayload payload, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
        => ScheduleAsync<TJob, TPayload>(payload, DateTimeOffset.UtcNow.Add(delay), cancellationToken);

    /// <inheritdoc />
    public async Task<string> ScheduleAsync<TJob, TPayload>(TPayload payload, DateTimeOffset runAt, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        var scheduler = await _factory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var (job, key) = BuildJob<TJob, TPayload>(payload);
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{key.Name}-trg", key.Group)
            .StartAt(runAt)
            .Build();
        await scheduler.ScheduleJob(job, trigger, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Quartz schedule {Job} key={Key} runAt={RunAt}", typeof(TJob).Name, key, runAt);
        return key.ToString();
    }

    /// <inheritdoc />
    public async Task AddOrUpdateRecurringAsync<TJob, TPayload>(string jobId, TPayload payload, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>
    {
        if (string.IsNullOrWhiteSpace(jobId)) throw new ArgumentException("jobId required", nameof(jobId));

        var scheduler = await _factory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var (job, key) = BuildJob<TJob, TPayload>(payload, jobId);
        var triggerKey = new TriggerKey($"{jobId}-trg", key.Group);

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .WithCronSchedule(cronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
            .ForJob(job)
            .Build();

        // Replace if exists.
        if (await scheduler.CheckExists(key, cancellationToken).ConfigureAwait(false))
            await scheduler.DeleteJob(key, cancellationToken).ConfigureAwait(false);

        await scheduler.ScheduleJob(job, trigger, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Quartz recurring {Job} key={Key} cron={Cron}", typeof(TJob).Name, key, cronExpression);
    }

    /// <inheritdoc />
    public async Task RemoveRecurringAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _factory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var key = new JobKey(jobId, RecurringGroup);
        if (await scheduler.CheckExists(key, cancellationToken).ConfigureAwait(false))
            await scheduler.DeleteJob(key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _factory.GetScheduler(cancellationToken).ConfigureAwait(false);
        // Try recurring group first, then try parsing as full key
        var key = new JobKey(jobId, RecurringGroup);
        if (await scheduler.CheckExists(key, cancellationToken).ConfigureAwait(false))
        {
            await scheduler.DeleteJob(key, cancellationToken).ConfigureAwait(false);
            return;
        }
        // Try as a raw key (group.name format from EnqueueAsync/ScheduleAsync)
        var parts = jobId.Split('.');
        if (parts.Length == 2)
        {
            key = new JobKey(parts[1], parts[0]);
            if (await scheduler.CheckExists(key, cancellationToken).ConfigureAwait(false))
                await scheduler.DeleteJob(key, cancellationToken).ConfigureAwait(false);
        }
    }
}
