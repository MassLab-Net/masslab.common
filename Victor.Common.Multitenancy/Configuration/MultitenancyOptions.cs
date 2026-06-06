namespace Victor.Common.Multitenancy.Configuration;

/// <summary>Options for tenant resolution.</summary>
public class MultitenancyOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Multitenancy";

    /// <summary>Tenant header name.</summary>
    public string HeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>Claim type containing the tenant id.</summary>
    public string ClaimType { get; set; } = "tenant_id";

    /// <summary>Base domain used by the sub-domain resolver.</summary>
    public string? BaseDomain { get; set; }

    /// <summary>Require a tenant for every request.</summary>
    public bool RequireTenant { get; set; }
}
