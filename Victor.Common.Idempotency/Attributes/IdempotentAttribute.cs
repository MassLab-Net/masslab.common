using Microsoft.AspNetCore.Mvc;

namespace Victor.Common.Idempotency.Attributes;

/// <summary>Marks an MVC action/controller as idempotent.</summary>
public sealed class IdempotentAttribute : TypeFilterAttribute
{
    /// <summary>Creates the filter attribute.</summary>
    public IdempotentAttribute() : base(typeof(Filters.IdempotentFilter))
    {
    }
}
