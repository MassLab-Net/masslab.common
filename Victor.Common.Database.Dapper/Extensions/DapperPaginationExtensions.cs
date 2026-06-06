using System.Data;
using Dapper;

namespace Victor.Common.Database.Dapper.Extensions;

/// <summary>
/// Extension methods for Dapper pagination support.
/// </summary>
/// <remarks>
/// The LIMIT/OFFSET syntax used here is PostgreSQL and MySQL specific.
/// For SQL Server, use OFFSET...FETCH NEXT or provider-specific pagination extensions.
/// This class is in the provider-agnostic package for convenience.
/// </remarks>
public static class DapperPaginationExtensions
{
    /// <summary>
    /// Executes a paginated query with total count.
    /// Uses two separate queries: one for count, one for data.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL query (without LIMIT/OFFSET).</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="param">The query parameters.</param>
    /// <param name="transaction">The transaction (optional).</param>
    /// <returns>A tuple containing the items and total count.</returns>
    public static async Task<(IEnumerable<T> Items, int TotalCount)> QueryPagedAsync<T>(
        this IDbConnection connection,
        string sql,
        int pageNumber,
        int pageSize,
        object? param = null,
        IDbTransaction? transaction = null)
    {
        var offset = (pageNumber - 1) * pageSize;

        // Build count query
        var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";

        // Build paginated query
        var pagedSql = $"{sql} LIMIT @PageSize OFFSET @Offset";

        // Combine parameters
        var parameters = new DynamicParameters(param);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        // Execute both queries
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param, transaction);
        var items = await connection.QueryAsync<T>(pagedSql, parameters, transaction);

        return (items, totalCount);
    }

    /// <summary>
    /// Executes a paginated query with total count using QueryMultiple for better performance.
    /// Executes both count and data queries in a single round-trip.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL query (without LIMIT/OFFSET).</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="param">The query parameters.</param>
    /// <param name="transaction">The transaction (optional).</param>
    /// <returns>A tuple containing the items and total count.</returns>
    public static async Task<(IEnumerable<T> Items, int TotalCount)> QueryPagedMultipleAsync<T>(
        this IDbConnection connection,
        string sql,
        int pageNumber,
        int pageSize,
        object? param = null,
        IDbTransaction? transaction = null)
    {
        var offset = (pageNumber - 1) * pageSize;

        // Build count query
        var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";

        // Build paginated query
        var pagedSql = $"{sql} LIMIT @PageSize OFFSET @Offset";

        // Combine parameters
        var parameters = new DynamicParameters(param);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        // Execute both queries in a single round-trip
        var batchSql = $"{countSql}; {pagedSql}";
        
        using var multi = await connection.QueryMultipleAsync(batchSql, parameters, transaction);
        
        var totalCount = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<T>();

        return (items, totalCount);
    }
}
