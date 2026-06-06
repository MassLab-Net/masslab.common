namespace MassLab.Common.Domain.Events;

/// <summary>
/// Represents a domain event raised inside the domain model.
/// Implementations should be immutable records.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC instant the event occurred.</summary>
    DateTimeOffset OccurredOn { get; }
}
