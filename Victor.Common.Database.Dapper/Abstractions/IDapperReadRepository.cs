using System.Data;
using Dapper;

namespace Victor.Common.Database.Dapper.Abstractions;

/// <summary>
/// Dapper repository for executing raw SQL read queries.
/// Uses read database connection.
/// </summary>
public interface IDapperReadRepository
{
    /// <summary>
    /// Sets an external connection to be used for all operations.
    /// Useful for sharing connection with EF Core.
    /// </summary>
    void SetConnection(IDbConnection connection, IDbTransaction? transaction = null);

    /// <summary>
    /// Clears the external connection, reverting to creating new connections.
    /// </summary>
    void ClearConnection();

    // Query operations
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<(IEnumerable<T> Items, int TotalCount)> QueryPagedAsync<T>(string sql, int pageNumber, int pageSize, object? param = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes multiple queries in a single round-trip and returns a grid reader.
    /// Use this for executing multiple queries efficiently.
    /// </summary>
    Task<TResult> QueryMultipleAsync<TResult>(string sql, Func<SqlMapper.GridReader, Task<TResult>> map, object? param = null, CancellationToken cancellationToken = default);
}
