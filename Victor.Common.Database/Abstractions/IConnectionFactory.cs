using System.Data;

namespace Victor.Common.Database.Abstractions;

/// <summary>
/// Defines the contract for creating and managing database connections.
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// Creates a new database connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A new database connection.</returns>
    IDbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Gets an existing connection or creates a new one if it doesn't exist.
    /// This method supports connection pooling and reuse.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>An existing or new database connection.</returns>
    IDbConnection GetOrCreateConnection(string connectionString);
}
