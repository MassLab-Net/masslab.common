using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Victor.Common.Domain.Entities;
using Victor.Common.Domain.Events;

namespace Victor.Common.Database.EFCore.Interceptors;

/// <summary>
/// After every successful <c>SaveChanges</c>, collects pending
/// <see cref="IDomainEvent"/>s from tracked <see cref="AggregateRoot"/>s
/// and dispatches them via the supplied delegate.
/// </summary>
/// <remarks>
/// The delegate is typically a thin wrapper around MediatR
/// (<c>(evt, ct) =&gt; mediator.Publish(evt, ct)</c>) but you can plug any
/// publishing mechanism (e.g. push to outbox, push to in-memory bus).
/// </remarks>
public class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly Func<IDomainEvent, CancellationToken, Task> _dispatch;

    /// <summary>Initializes a new instance.</summary>
    public DomainEventDispatchInterceptor(Func<IDomainEvent, CancellationToken, Task> dispatch)
    {
        _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        // No-op: domain event dispatch is only performed in the async path
        // to avoid deadlocks from .GetAwaiter().GetResult().
        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null) return;

        var aggregates = context.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0) return;

        // Capture and clear before dispatching so handlers raising more events
        // don't cause re-entry into the same flush cycle.
        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var agg in aggregates) agg.ClearDomainEvents();

        foreach (var domainEvent in events)
        {
            await _dispatch(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
