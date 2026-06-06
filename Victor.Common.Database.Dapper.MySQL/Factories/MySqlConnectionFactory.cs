using System.Collections.Concurrent;
using System.Data;
using MySqlConnector;
using Victor.Common.Database.Abstractions;

namespace Victor.Common.Database.Dapper.MySQL.Factories;

/// <summary>
/// MySQL implementation of IConnectionFactory for Dapper.
/// </summary>
public class MySqlConnectionFactory : IConnectionFactory
{
    private readonly ConcurrentDictionary<string, IDbConnection> _connections = new();

    /// <summary>
    /// Creates a new MySQL database connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A new MySqlConnection instance.</returns>
    public IDbConnection CreateConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        return new MySqlConnection(connectionString);
    }

    /// <summary>
    /// Gets or creates a cached MySQL database connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A cached or new MySqlConnection instance.</returns>
    public IDbConnection GetOrCreateConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        return _connections.GetOrAdd(connectionString, cs => CreateConnection(cs));
    }
}
