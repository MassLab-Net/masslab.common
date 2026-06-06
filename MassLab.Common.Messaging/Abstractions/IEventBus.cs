namespace MassLab.Common.Messaging.Abstractions;

/// <summary>
/// Marker interface for cross-service integration events. Implementations
/// should be immutable records carrying primitive / serializable data.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique event identifier (used for idempotent delivery).</summary>
    Guid EventId { get; }

    /// <summary>UTC instant the event was raised.</summary>
    DateTimeOffset OccurredOn { get; }
}

/// <summary>
/// Convenience base class for integration events.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Handles a single integration event type. Multiple handlers per event are
/// allowed; the event bus dispatches to all of them.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    /// <summary>Handles the integration event.</summary>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes integration events to the configured broker.
/// </summary>
public interface IEventBus
{
    /// <summary>Publishes an integration event.</summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    /// <summary>Publishes an integration event using its runtime type.</summary>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
