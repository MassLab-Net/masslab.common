using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Victor.Common.Domain.Auditing;

namespace Victor.Common.Database.EFCore.Interceptors;

/// <summary>
/// Intercepts deletes of <see cref="ISoftDeletable"/> entities and converts
/// them to soft-deletes (<c>IsDeleted=true</c>, <c>DeletedAt=now</c>) instead
/// of issuing physical <c>DELETE</c> statements.
/// </summary>
/// <remarks>
/// Callers should also configure a global query filter so soft-deleted rows
/// are excluded from reads:
/// <code>
/// modelBuilder.Entity&lt;Product&gt;().HasQueryFilter(p =&gt; !p.IsDeleted);
/// </code>
/// </remarks>
public class SoftDeleteSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly Func<DateTime> _now;

    /// <summary>Initializes a new instance.</summary>
    public SoftDeleteSaveChangesInterceptor(Func<DateTime>? clock = null)
        => _now = clock ?? (() => DateTime.UtcNow);

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Convert(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Convert(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Convert(DbContext? context)
    {
        if (context is null) return;
        var now = _now();

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
            }
        }
    }
}
