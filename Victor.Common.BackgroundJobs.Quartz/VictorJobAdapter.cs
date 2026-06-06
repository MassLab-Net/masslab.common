using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Victor.Common.BackgroundJobs.Abstractions;

namespace Victor.Common.BackgroundJobs.Quartz;

/// <summary>
/// Quartz <see cref="IJob"/> adapter that pulls the JSON payload from
/// <see cref="JobDataMap"/>, resolves the user's <typeparamref name="TJob"/>
/// from DI, and invokes <c>ExecuteAsync</c>.
/// </summary>
[DisallowConcurrentExecution]
public class VictorJobAdapter<TJob, TPayload> : IJob
    where TJob : IBackgroundJob<TPayload>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VictorJobAdapter<TJob, TPayload>> _logger;

    /// <summary>Initializes a new instance.</summary>
    public VictorJobAdapter(
        IServiceProvider serviceProvider,
        ILogger<VictorJobAdapter<TJob, TPayload>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var data = context.MergedJobDataMap;
        var json = data.GetString(QuartzBackgroundJobScheduler.PayloadKey);
        if (string.IsNullOrEmpty(json))
        {
            _logger.LogWarning("Quartz job {Key} missing payload", context.JobDetail.Key);
            return;
        }

        TPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TPayload>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quartz job {Key} payload deserialization failed", context.JobDetail.Key);
            throw;
        }

        if (payload is null)
        {
            _logger.LogWarning("Quartz job {Key} payload was null", context.JobDetail.Key);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TJob>();
        await job.ExecuteAsync(payload, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Adapter without [DisallowConcurrentExecution] for jobs implementing <see cref="IConcurrentJob"/>.
/// </summary>
public class ConcurrentVictorJobAdapter<TJob, TPayload> : IJob
    where TJob : IBackgroundJob<TPayload>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConcurrentVictorJobAdapter<TJob, TPayload>> _logger;

    public ConcurrentVictorJobAdapter(
        IServiceProvider serviceProvider,
        ILogger<ConcurrentVictorJobAdapter<TJob, TPayload>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var data = context.MergedJobDataMap;
        var json = data.GetString(QuartzBackgroundJobScheduler.PayloadKey);
        if (string.IsNullOrEmpty(json))
        {
            _logger.LogWarning("Quartz job {Key} missing payload", context.JobDetail.Key);
            return;
        }

        TPayload? payload;
        try { payload = JsonSerializer.Deserialize<TPayload>(json); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quartz job {Key} payload deserialization failed", context.JobDetail.Key);
            throw;
        }

        if (payload is null) { _logger.LogWarning("Quartz job {Key} payload was null", context.JobDetail.Key); return; }

        using var scope = _serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TJob>();
        await job.ExecuteAsync(payload, context.CancellationToken).ConfigureAwait(false);
    }
}
