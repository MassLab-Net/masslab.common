namespace MassLab.Common.BackgroundJobs.Abstractions;

/// <summary>
/// Interface for background jobs that require no payload.
/// </summary>
public interface IBackgroundJob
{
    /// <summary>Executes the job.</summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface indicating the job allows concurrent execution.
/// Jobs implementing this will NOT have [DisallowConcurrentExecution] applied.
/// </summary>
public interface IConcurrentJob
{
}

/// <summary>
/// A background job that accepts a strongly-typed payload.
/// </summary>
/// <typeparam name="TPayload">Payload type. Must be JSON-serializable.</typeparam>
public interface IBackgroundJob<in TPayload>
{
    /// <summary>Executes the job.</summary>
    Task ExecuteAsync(TPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface to bootstrap recurring jobs at startup. Implementations
/// should register themselves with <see cref="IBackgroundJobScheduler"/>
/// during application initialization (see provider extensions).
/// </summary>
public interface IRecurringJobBootstrapper
{
    /// <summary>Register all recurring jobs with the scheduler.</summary>
    Task RegisterAsync(IBackgroundJobScheduler scheduler, CancellationToken cancellationToken = default);
}

/// <summary>
/// Background-job scheduler abstraction. Implementations: Hangfire / Quartz.
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>Fire-and-forget execution.</summary>
    Task<string> EnqueueAsync<TJob, TPayload>(TPayload payload, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>;

    /// <summary>Schedule the job to run after the supplied delay.</summary>
    Task<string> ScheduleAsync<TJob, TPayload>(TPayload payload, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>;

    /// <summary>Schedule the job to run at the supplied UTC instant.</summary>
    Task<string> ScheduleAsync<TJob, TPayload>(TPayload payload, DateTimeOffset runAt, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>;

    /// <summary>Register or update a recurring job using a cron expression (UTC).</summary>
    Task AddOrUpdateRecurringAsync<TJob, TPayload>(string jobId, TPayload payload, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : IBackgroundJob<TPayload>;

    /// <summary>Removes a previously-registered recurring job.</summary>
    Task RemoveRecurringAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>Cancels/deletes a scheduled or enqueued job by its identifier.</summary>
    Task CancelAsync(string jobId, CancellationToken cancellationToken = default);
}
