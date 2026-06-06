namespace MassLab.Common.Domain.Entities;

/// <summary>
/// Base class for value objects: equality is based on the value of their
/// components, not on identity. Subclasses must override
/// <see cref="GetEqualityComponents"/>.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Returns the components that participate in value equality.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other) =>
        other is not null
        && GetType() == other.GetType()
        && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValueObject vo && Equals(vo);

    /// <inheritdoc />
    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(17, (acc, c) => HashCode.Combine(acc, c));

    /// <summary>Equality operator.</summary>
    public static bool operator ==(ValueObject? a, ValueObject? b) => Equals(a, b);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(ValueObject? a, ValueObject? b) => !Equals(a, b);
}
