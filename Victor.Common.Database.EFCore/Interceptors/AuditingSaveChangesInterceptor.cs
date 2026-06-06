using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Victor.Common.Domain.Auditing;

namespace Victor.Common.Database.EFCore.Interceptors;

/// <summary>
/// Reads the current user (via an injected delegate) and stamps
/// <see cref="IAuditable.CreatedAt"/>/<see cref="IAuditable.CreatedBy"/> /
/// <see cref="IAuditable.UpdatedAt"/>/<see cref="IAuditable.UpdatedBy"/> on
/// added or modified entities before <c>SaveChanges</c> is committed.
/// </summary>
public class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly Func<string?> _currentUser;
    private readonly Func<DateTime> _now;

    /// <summary>
    /// Initializes a new instance of <see cref="AuditingSaveChangesInterceptor"/>.
    /// </summary>
    /// <param name="currentUser">Resolves the current user identifier (e.g. <c>ICurrentUser.UserId.ToString()</c>). May return null.</param>
    /// <param name="clock">Optional clock; defaults to <see cref="DateTime.UtcNow"/>.</param>
    public AuditingSaveChangesInterceptor(Func<string?> currentUser, Func<DateTime>? clock = null)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _now = clock ?? (() => DateTime.UtcNow);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StampAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampAudit(DbContext? context)
    {
        if (context is null) return;
        var now = _now();
        var user = _currentUser();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = entry.Entity.CreatedAt == default ? now : entry.Entity.CreatedAt;
                    entry.Entity.CreatedBy ??= user;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = user;
                    // CreatedAt/By are immutable post-creation
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                    break;
            }
        }
    }
}
