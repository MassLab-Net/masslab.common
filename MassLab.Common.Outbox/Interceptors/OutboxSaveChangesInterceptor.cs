using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MassLab.Common.Domain.Entities;
using MassLab.Common.Outbox.Entities;

namespace MassLab.Common.Outbox.Interceptors;

/// <summary>
/// EFCore <see cref="SaveChangesInterceptor"/> that captures domain events
/// from tracked <see cref="AggregateRoot"/> instances into the
/// <see cref="OutboxMessage"/> table — inside the same transaction as the
/// originating <c>SaveChanges</c>.
/// </summary>
/// <remarks>
/// The DbContext must register an <c>OutboxMessage</c> DbSet (or apply
/// <c>OutboxMessageConfiguration</c>). The interceptor runs in
/// <c>SavingChanges</c> (before commit) so the outbox row participates in
/// the same DB transaction.
/// </remarks>
public class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Capture(DbContext? context)
    {
        if (context is null) return;

        var outboxSet = context.Set<OutboxMessage>();

        var aggregates = context.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var type = domainEvent.GetType();
                var payload = JsonSerializer.Serialize(domainEvent, type, JsonOpts);
                outboxSet.Add(new OutboxMessage
                {
                    Type = $"{type.FullName}, {type.Assembly.GetName().Name}",
                    Payload = payload,
                    OccurredOn = domainEvent.OccurredOn.UtcDateTime,
                });
            }
            aggregate.ClearDomainEvents();
        }
    }
}
