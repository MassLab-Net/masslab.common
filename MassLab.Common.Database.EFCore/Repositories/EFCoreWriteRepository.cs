using Microsoft.EntityFrameworkCore;
using MassLab.Common.Database.Abstractions;

namespace MassLab.Common.Database.EFCore.Repositories;

/// <summary>
/// Entity Framework Core implementation of write repository.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class EFCoreWriteRepository<TEntity> : IWriteRepository<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreWriteRepository{TEntity}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EFCoreWriteRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }

    /// <inheritdoc />
    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var entry = await _dbSet.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    /// <inheritdoc />
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        await _dbSet.AddRangeAsync(entities, ct);
    }

    /// <inheritdoc />
    public Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }
}
