namespace MassLab.Common.Multitenancy.Abstractions;

/// <summary>Scoped tenant context.</summary>
public sealed class TenantContext : ITenantContext
{
    /// <inheritdoc />
    public Guid? TenantId { get; private set; }

    /// <inheritdoc />
    public bool HasTenant => TenantId.HasValue;

    /// <inheritdoc />
    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));

        TenantId = tenantId;
    }
}
