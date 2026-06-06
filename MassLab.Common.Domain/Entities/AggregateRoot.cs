using MassLab.Common.Domain.Events;

namespace MassLab.Common.Domain.Entities;

/// <summary>
/// Base class for aggregate roots. Tracks domain events raised during the
/// aggregate's lifetime; the persistence layer flushes them after a successful
/// transaction (see <c>DomainEventDispatchInterceptor</c>).
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private IReadOnlyCollection<IDomainEvent>? _readOnlyEvents;

    /// <summary>Domain events recorded since the last flush.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _readOnlyEvents ??= _domainEvents.AsReadOnly();

    /// <summary>Records a domain event.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is null) throw new ArgumentNullException(nameof(domainEvent));
        _domainEvents.Add(domainEvent);
        _readOnlyEvents = null;
    }

    /// <summary>Removes a previously-raised domain event.</summary>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
        _readOnlyEvents = null;
    }

    /// <summary>Clears all recorded domain events (called after dispatch).</summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
        _readOnlyEvents = null;
    }

    /// <inheritdoc />
    protected AggregateRoot() { }

    /// <inheritdoc />
    protected AggregateRoot(Guid id) : base(id) { }
}
