using System.Linq.Expressions;
using MassLab.Common.Domain.Specifications;

namespace MassLab.Common.Database.Abstractions;

/// <summary>
/// Defines the contract for read operations on entities with a custom key type.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public interface IReadRepository<TEntity, in TKey> where TEntity : class
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for read operations on entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IReadRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all entities.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of entities matching the predicate.</returns>
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of entities.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the items and total count.</returns>
    Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities that match the specified predicate with pagination.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the items and total count.</returns>
    Task<(IEnumerable<TEntity> Items, int TotalCount)> FindPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entities matching the supplied <see cref="ISpecification{T}"/>.
    /// </summary>
    /// <param name="specification">The specification (filter, includes, ordering, paging).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching entities (in spec order, paged when configured).</returns>
    /// <remarks>
    /// Default-implemented using <see cref="FindAsync"/> for backward
    /// compatibility; EFCore implementations override this to translate the
    /// specification into a single SQL query.
    /// </remarks>
    Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        // Default fallback: filter + in-memory paging/ordering.
        // Concrete implementations should override for efficient SQL.
        return ListAsyncFallback(specification, cancellationToken);
    }

    /// <summary>
    /// Counts entities matching the optional predicate.
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>
    /// Checks whether any entity matches the predicate.
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    private async Task<IReadOnlyList<TEntity>> ListAsyncFallback(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken)
    {
        var rows = await FindAsync(specification.Criteria, cancellationToken);
        IEnumerable<TEntity> q = rows;

        if (specification.OrderBy is not null)
        {
            var compiled = specification.OrderBy.Compile();
            q = specification.OrderDescending
                ? q.OrderByDescending(compiled)
                : q.OrderBy(compiled);
        }

        if (specification.Skip is int s) q = q.Skip(s);
        if (specification.Take is int t) q = q.Take(t);

        return q.ToList();
    }
}
