using System.Data;
using Dapper;
using MassLab.Common.Database.Abstractions;
using MassLab.Common.Database.Dapper.Abstractions;

namespace MassLab.Common.Database.Dapper.Repositories;

/// <summary>
/// Dapper repository for executing raw SQL read queries.
/// Uses read database connection.
/// </summary>
public class DapperReadRepository : IDapperReadRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _connectionString;
    
    // External connection/transaction support
    private IDbConnection? _externalConnection;
    private IDbTransaction? _externalTransaction;

    public DapperReadRepository(IConnectionFactory connectionFactory, string connectionString)
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
                if (_externalConnection is System.Data.Common.DbConnection dbConn)
                    await dbConn.OpenAsync(cancellationToken);
                else
                    _externalConnection.Open();
            }
            return (_externalConnection, _externalTransaction, false);
        }

        var connection = _connectionFactory.CreateConnection(_connectionString);
        if (connection is System.Data.Common.DbConnection dbConnection)
            await dbConnection.OpenAsync(cancellationToken);
        else
            connection.Open();
        return (connection, null, true);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction, shouldDispose) = await GetConnectionAsync(cancellationToken);
        try
        {
            return await connection.QueryAsync<T>(
                new CommandDefinition(sql, param, transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction, shouldDispose) = await GetConnectionAsync(cancellationToken);
        try
        {
            return await connection.QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(sql, param, transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction, shouldDispose) = await GetConnectionAsync(cancellationToken);
        try
        {
            return await connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, param, transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<T> Items, int TotalCount)> QueryPagedAsync<T>(
        string sql,
        int pageNumber,
        int pageSize,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction, shouldDispose) = await GetConnectionAsync(cancellationToken);
        try
        {
            var offset = (pageNumber - 1) * pageSize;

            var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";
            var pagedSql = $"{sql} LIMIT @PageSize OFFSET @Offset";

            var parameters = new DynamicParameters(param);
            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", offset);

            var batchSql = $"{countSql}; {pagedSql}";
            
            using var multi = await connection.QueryMultipleAsync(
                new CommandDefinition(batchSql, parameters, transaction, cancellationToken: cancellationToken));
            
            var totalCount = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<T>();

            return (items, totalCount);
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<TResult> QueryMultipleAsync<TResult>(
        string sql,
        Func<SqlMapper.GridReader, Task<TResult>> map,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction, shouldDispose) = await GetConnectionAsync(cancellationToken);
        try
        {
            using var multi = await connection.QueryMultipleAsync(
                new CommandDefinition(sql, param, transaction, cancellationToken: cancellationToken));
            
            return await map(multi);
        }
        finally
        {
            if (shouldDispose) connection.Dispose();
        }
    }
}
