namespace Victor.Common.Domain.Auditing;

/// <summary>
/// Marker interface for entities that track creation / update audit data.
/// Populated automatically by EFCore <c>AuditingSaveChangesInterceptor</c>.
/// </summary>
public interface IAuditable
{
    /// <summary>UTC instant the entity was created.</summary>
    DateTime CreatedAt { get; set; }

    /// <summary>UTC instant of last update; null until first update.</summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>User identifier of creator.</summary>
    string? CreatedBy { get; set; }

    /// <summary>User identifier of last updater.</summary>
    string? UpdatedBy { get; set; }
}

/// <summary>
/// Marker interface for entities supporting soft-delete. The
/// <c>SoftDeleteSaveChangesInterceptor</c> intercepts deletes and toggles
/// <see cref="IsDeleted"/>+<see cref="DeletedAt"/> instead of removing rows.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>True when the entity has been logically deleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>UTC instant of soft-delete.</summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>User identifier of the deleter.</summary>
    string? DeletedBy { get; set; }
}

/// <summary>
/// Marker interface for entities that belong to a tenant.
/// Used by <c>Victor.Common.Multitenancy</c> to apply a global query filter.
/// </summary>
public interface ITenantOwned
{
    /// <summary>The tenant identifier.</summary>
    Guid TenantId { get; set; }
}
