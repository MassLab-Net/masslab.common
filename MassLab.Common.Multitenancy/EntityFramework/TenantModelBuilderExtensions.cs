using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MassLab.Common.Domain.Auditing;

namespace MassLab.Common.Multitenancy.EntityFramework;

/// <summary>
/// Interface that a DbContext must implement to support tenant query filters.
/// EF Core will re-evaluate the <see cref="TenantId"/> property on each query
/// because the filter expression references the DbContext field directly.
/// </summary>
public interface ITenantDbContext
{
    /// <summary>Current tenant id (null = no filtering).</summary>
    Guid? TenantId { get; }
}

/// <summary>EFCore model-builder helpers for tenant-owned entities.</summary>
public static class TenantModelBuilderExtensions
{
    /// <summary>
    /// Applies a global query filter to each <see cref="ITenantOwned"/> entity type.
    /// The filter references the DbContext's <c>TenantId</c> property via a field
    /// member expression, which EF Core re-evaluates per query (not cached with the model).
    /// </summary>
    public static ModelBuilder ApplyTenantQueryFilters<TContext>(this ModelBuilder modelBuilder, TContext dbContext)
        where TContext : DbContext, ITenantDbContext
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(ITenantOwned).IsAssignableFrom(t.ClrType)))
        {
            var method = typeof(TenantModelBuilderExtensions)
                .GetMethod(nameof(SetFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType, typeof(TContext));
            method.Invoke(null, [modelBuilder, dbContext]);
        }

        return modelBuilder;
    }

    /// <summary>
    /// Overload accepting a plain DbContext for backward compatibility.
    /// The DbContext must implement <see cref="ITenantDbContext"/>.
    /// </summary>
    public static ModelBuilder ApplyTenantQueryFilters(this ModelBuilder modelBuilder, DbContext dbContext)
    {
        if (dbContext is not ITenantDbContext)
            throw new InvalidOperationException(
                $"DbContext '{dbContext.GetType().Name}' must implement ITenantDbContext to use tenant query filters.");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(ITenantOwned).IsAssignableFrom(t.ClrType)))
        {
            var method = typeof(TenantModelBuilderExtensions)
                .GetMethod(nameof(SetFilterDynamic), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType, dbContext.GetType());
            method.Invoke(null, [modelBuilder, dbContext]);
        }

        return modelBuilder;
    }

    private static void SetFilter<TEntity, TContext>(ModelBuilder modelBuilder, TContext dbContext)
        where TEntity : class, ITenantOwned
        where TContext : DbContext, ITenantDbContext
    {
        // EF Core captures the dbContext reference (not its current value).
        // Because the filter lambda closes over the dbContext instance field,
        // EF re-reads TenantId on every query execution.
        modelBuilder.Entity<TEntity>().HasQueryFilter(
            entity => dbContext.TenantId == null || entity.TenantId == dbContext.TenantId!.Value);
    }

    private static void SetFilterDynamic<TEntity, TContext>(ModelBuilder modelBuilder, object dbContext)
        where TEntity : class, ITenantOwned
        where TContext : DbContext, ITenantDbContext
    {
        SetFilter<TEntity, TContext>(modelBuilder, (TContext)dbContext);
    }
}
