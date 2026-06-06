using System.Collections.Concurrent;
using System.Data;
using Npgsql;
using MassLab.Common.Database.Abstractions;

namespace MassLab.Common.Database.Dapper.PostgreSQL.Factories;

/// <summary>
/// PostgreSQL implementation of IConnectionFactory.
/// Supports explicit connection reuse for callers that use GetOrCreateConnection.
/// </summary>
public class PostgreSqlConnectionFactory : IConnectionFactory
{
    private readonly ConcurrentDictionary<string, IDbConnection> _connections = new();

    /// <inheritdoc />
    public IDbConnection CreateConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        return new NpgsqlConnection(connectionString);
    }

    /// <inheritdoc />
    public IDbConnection GetOrCreateConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        return _connections.GetOrAdd(connectionString, cs => CreateConnection(cs));
    }
}
