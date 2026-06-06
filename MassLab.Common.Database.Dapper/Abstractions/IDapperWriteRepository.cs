using System.Data;

namespace MassLab.Common.Database.Dapper.Abstractions;

/// <summary>
/// Dapper repository for executing raw SQL write commands.
/// Uses write database connection.
/// </summary>
public interface IDapperWriteRepository
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

    // Command operations
    Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
}
