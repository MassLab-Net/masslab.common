namespace Victor.Common.Multitenancy.Abstractions;

/// <summary>Current request tenant context.</summary>
public interface ITenantContext
{
    /// <summary>Resolved tenant id, if any.</summary>
    Guid? TenantId { get; }

    /// <summary>True when a tenant was resolved.</summary>
    bool HasTenant { get; }

    /// <summary>Sets the current tenant id.</summary>
    void SetTenant(Guid tenantId);
}
