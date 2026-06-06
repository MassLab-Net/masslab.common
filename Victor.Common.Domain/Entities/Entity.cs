namespace Victor.Common.Domain.Entities;

/// <summary>
/// Base class for entities identified by a <see cref="Guid"/>.
/// Two entities are equal when they share the same <see cref="Id"/>.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>The unique identifier of the entity.</summary>
    public Guid Id { get; protected set; }

    /// <summary>Initializes a new entity with a generated identifier.</summary>
    protected Entity() : this(Guid.NewGuid()) { }

    /// <summary>Initializes a new entity with the supplied identifier.</summary>
    protected Entity(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Entity Id cannot be empty.", nameof(id));
        Id = id;
    }

    /// <inheritdoc />
    public bool Equals(Entity? other) =>
        other is not null && (ReferenceEquals(this, other) || (GetType() == other.GetType() && other.Id == Id));

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Entity e && Equals(e);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Entity? left, Entity? right) =>
        Equals(left, right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Entity? left, Entity? right) =>
        !Equals(left, right);
}
