using Microsoft.EntityFrameworkCore;

namespace Victor.Common.Database.EFCore.Extensions;

/// <summary>
/// Extension methods for IQueryable to support pagination.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies pagination to a queryable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The queryable.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>The paginated queryable.</returns>
    public static IQueryable<T> Paginate<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize)
    {
        var skip = (pageNumber - 1) * pageSize;
        return query.Skip(skip).Take(pageSize);
    }

    /// <summary>
    /// Gets a paginated result with total count.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The queryable.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the items and total count.</returns>
    public static async Task<(List<T> Items, int TotalCount)> ToPagedListAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Paginate(pageNumber, pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
