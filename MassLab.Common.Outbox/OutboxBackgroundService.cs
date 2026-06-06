using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MassLab.Common.Caching.Abstractions;
using MassLab.Common.Domain.Events;
using MassLab.Common.Messaging.Abstractions;
using MassLab.Common.Outbox.Configuration;
using MassLab.Common.Outbox.Entities;

namespace MassLab.Common.Outbox;

/// <summary>
/// Polls the outbox table for unprocessed messages and dispatches each one
/// via the registered <see cref="IEventBus"/>. Successfully-dispatched
/// messages are marked <c>ProcessedOn</c> (or deleted, depending on options);
/// failures are retried with attempt counter.
/// </summary>
/// <typeparam name="TDbContext">The application's DbContext that owns the outbox table.</typeparam>
public class OutboxBackgroundService<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxBackgroundService<TDbContext>> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Initializes a new instance.</summary>
    public OutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxBackgroundService<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher for {Context} started (poll={Interval}, batch={Batch})",
            typeof(TDbContext).Name, _options.PollingInterval, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var distributedLock = scope.ServiceProvider.GetService<IDistributedLock>();
                if (distributedLock is not null)
                {
                    var lockToken = await distributedLock.AcquireLockAsync(
                        $"outbox:{typeof(TDbContext).Name}",
                        TimeSpan.FromSeconds(5),
                        _options.PollingInterval + TimeSpan.FromSeconds(5),
                        stoppingToken).ConfigureAwait(false);
                    if (lockToken is null)
                    {
                        _logger.LogDebug("Outbox lock not acquired, skipping iteration");
                    }
                    else
                    {
                        try { await DispatchPendingAsync(stoppingToken).ConfigureAwait(false); }
                        finally { await distributedLock.ReleaseLockAsync(lockToken, stoppingToken).ConfigureAwait(false); }
                    }
                }
                else
                {
                    await DispatchPendingAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher iteration failed");
            }

            try { await Task.Delay(_options.PollingInterval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var bus     = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var batch = await context.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null && m.Attempts < _options.MaxAttempts
                && (m.NextAttemptAt == null || m.NextAttemptAt <= DateTime.UtcNow))
            .OrderBy(m => m.OccurredOn)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (batch.Count == 0) return;

        foreach (var message in batch)
        {
            try
            {
                var type = ResolveType(message.Type);
                if (type is null)
                {
                    MarkFailed(message, $"Unknown type: {message.Type}");
                    continue;
                }

                if (JsonSerializer.Deserialize(message.Payload, type, JsonOpts) is not { } payload)
                {
                    MarkFailed(message, "Failed to deserialize payload");
                    continue;
                }

                if (payload is IIntegrationEvent integration)
                {
                    await bus.PublishAsync(integration, cancellationToken).ConfigureAwait(false);
                }
                else if (payload is IDomainEvent)
                {
                    // For pure domain events, we wrap them into an envelope only if
                    // an IEventBus serializer expects IIntegrationEvent. As a fallback,
                    // we treat them as already-handled.
                    _logger.LogDebug("Outbox skipped pure IDomainEvent {Type} (no integration adapter)", message.Type);
                }

                if (_options.DeleteAfterDispatch)
                {
                    context.Set<OutboxMessage>().Remove(message);
                }
                else
                {
                    message.ProcessedOn = DateTime.UtcNow;
                    message.Error = null;
                }
            }
            catch (Exception ex)
            {
                MarkFailed(message, ex.Message);
                _logger.LogWarning(ex, "Outbox dispatch failed (attempt {Attempt}/{Max}) for {Type}",
                    message.Attempts, _options.MaxAttempts, message.Type);
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Cleanup: remove old processed messages beyond retention period
        if (_options.RetentionDays > 0 && !_options.DeleteAfterDispatch)
        {
            var cutoff = DateTime.UtcNow.AddDays(-_options.RetentionDays);
            var stale = await context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOn != null && m.ProcessedOn < cutoff)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (stale.Count > 0)
            {
                context.Set<OutboxMessage>().RemoveRange(stale);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static Type? ResolveType(string typeName)
    {
        var t = Type.GetType(typeName, throwOnError: false);
        if (t is not null) return t;
        // Try loaded assemblies (handles assembly-qualified names that don't fully load).
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(typeName.Split(',')[0], throwOnError: false))
            .FirstOrDefault(x => x is not null);
    }

    private static string Truncate(string s, int maxLen)
        => s.Length <= maxLen ? s : s.Substring(0, maxLen);

    private static void MarkFailed(OutboxMessage message, string error)
    {
        message.Attempts++;
        message.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, message.Attempts));
        message.Error = Truncate(error, 1900);
    }
}
