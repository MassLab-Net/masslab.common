using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using MassLab.Common.Database.Abstractions;

namespace MassLab.Common.Database.EFCore.SqlServer.Factories;

/// <summary>
/// SQL Server implementation of IConnectionFactory that creates and manages SqlConnection instances.
/// </summary>
public class SqlServerConnectionFactory : IConnectionFactory
{
    private readonly ConcurrentDictionary<string, IDbConnection> _connections = new();

    /// <summary>
    /// Creates a new SqlConnection instance.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>A new SqlConnection instance.</returns>
    /// <exception cref="ArgumentException">Thrown when connectionString is null or empty.</exception>
    public IDbConnection CreateConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        return new SqlConnection(connectionString);
    }

    /// <summary>
    /// Gets an existing SqlConnection or creates a new one if it doesn't exist.
    /// This method supports connection pooling and reuse through caching.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>An existing or new SqlConnection instance.</returns>
    /// <exception cref="ArgumentException">Thrown when connectionString is null or empty.</exception>
    public IDbConnection GetOrCreateConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        return _connections.GetOrAdd(connectionString, cs => CreateConnection(cs));
    }
}
