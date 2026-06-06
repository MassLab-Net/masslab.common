using System.Data;
using Dapper;
using MassLab.Common.Database.Abstractions;
using MassLab.Common.Database.Dapper.Abstractions;

namespace MassLab.Common.Database.Dapper.Repositories;

/// <summary>
/// Dapper repository for executing raw SQL write commands.
/// Uses write database connection.
/// </summary>
public class DapperWriteRepository : IDapperWriteRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _connectionString;
    
    // External connection/transaction support
    private IDbConnection? _externalConnection;
    private IDbTransaction? _externalTransaction;

    public DapperWriteRepository(IConnectionFactory connectionFactory, string connectionString)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <inheritdoc />
    public void SetConnection(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _externalConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        _externalTransaction = transaction;
    }

    /// <inheritdoc />
    public void ClearConnection()
    {
        _externalConnection = null;
        _externalTransaction = null;
    }

    private async Task<(IDbConnection Connection, IDbTransaction? Transaction, bool ShouldDispose)> GetConnectionAsync(
        CancellationToken cancellationToken)
    {
        if (_externalConnection != null)
        {
            if (_externalConnection.State != ConnectionState.Open)
            {
                _externalConnection.Open();
            }
            return (_externalConnection, _externalTransaction, false);
        }

        var connection = _connectionFactory.CreateConnection(_connectionString);
        connection.Open();
        return (connection, null, true);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction, shouldDispose) = await GetConnectionAsync(cancellationToken);
        try
        {
            return await connection.ExecuteAsync(
                new CommandDefinition(sql, param, transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction, shouldDispose) = await GetConnectionAsync(cancellationToken);
        try
        {
            return await connection.ExecuteScalarAsync<T>(
                new CommandDefinition(sql, param, transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }
}
