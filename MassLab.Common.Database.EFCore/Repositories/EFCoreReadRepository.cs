using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MassLab.Common.Database.Abstractions;
using MassLab.Common.Database.EFCore.Extensions;
using MassLab.Common.Database.EFCore.Specifications;
using MassLab.Common.Domain.Specifications;

namespace MassLab.Common.Database.EFCore.Repositories;

/// <summary>
/// Entity Framework Core implementation of read repository.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class EFCoreReadRepository<TEntity> : IReadRepository<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreReadRepository{TEntity}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EFCoreReadRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();
        var (items, totalCount) = await query.ToPagedListAsync(pageNumber, pageSize, cancellationToken);
        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<TEntity> Items, int TotalCount)> FindPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(predicate);
        var (items, totalCount) = await query.ToPagedListAsync(pageNumber, pageSize, cancellationToken);
        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.Apply(_dbSet.AsQueryable(), specification);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{TEntity}"/> for building advanced LINQ queries.
    /// Tracking is disabled by default to keep reads side-effect-free.
    /// </summary>
    public virtual IQueryable<TEntity> AsQueryable()
    {
        return _dbSet.AsNoTracking();
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        return predicate is null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(predicate, ct);
    }
}
