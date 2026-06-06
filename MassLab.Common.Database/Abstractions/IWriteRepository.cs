namespace MassLab.Common.Database.Abstractions;

/// <summary>
/// Defines the contract for write operations on entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IWriteRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added entity.</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the repository.
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    /// <summary>
    /// Deletes multiple entities from the repository.
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
}
