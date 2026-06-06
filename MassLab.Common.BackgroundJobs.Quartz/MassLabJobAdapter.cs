using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using MassLab.Common.BackgroundJobs.Abstractions;

namespace MassLab.Common.BackgroundJobs.Quartz;

/// <summary>
/// Quartz <see cref="IJob"/> adapter that pulls the JSON payload from
/// <see cref="JobDataMap"/>, resolves the user's <typeparamref name="TJob"/>
/// from DI, and invokes <c>ExecuteAsync</c>.
/// </summary>
[DisallowConcurrentExecution]
public class MassLabJobAdapter<TJob, TPayload> : IJob
    where TJob : IBackgroundJob<TPayload>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MassLabJobAdapter<TJob, TPayload>> _logger;

    /// <summary>Initializes a new instance.</summary>
    public MassLabJobAdapter(
        IServiceProvider serviceProvider,
        ILogger<MassLabJobAdapter<TJob, TPayload>> logger)
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
public class ConcurrentMassLabJobAdapter<TJob, TPayload> : IJob
    where TJob : IBackgroundJob<TPayload>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConcurrentMassLabJobAdapter<TJob, TPayload>> _logger;

    public ConcurrentMassLabJobAdapter(
        IServiceProvider serviceProvider,
        ILogger<ConcurrentMassLabJobAdapter<TJob, TPayload>> logger)
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
