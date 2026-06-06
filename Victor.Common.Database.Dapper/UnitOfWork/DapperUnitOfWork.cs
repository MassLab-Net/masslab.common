using System.Data;
using Victor.Common.Database.Abstractions;

namespace Victor.Common.Database.Dapper.UnitOfWork;

/// <summary>
/// Dapper implementation of Unit of Work pattern using ADO.NET transactions.
/// </summary>
public class DapperUnitOfWork : IUnitOfWork
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _connectionString;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperUnitOfWork"/> class.
    /// </summary>
    /// <param name="connectionFactory">The connection factory.</param>
    /// <param name="connectionString">The connection string.</param>
    public DapperUnitOfWork(IConnectionFactory connectionFactory, string connectionString)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Gets the current transaction.
    /// </summary>
    public IDbTransaction? Transaction => _transaction;

    /// <summary>
    /// Gets the current connection.
    /// </summary>
    public IDbConnection? Connection => _connection;

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes commands immediately, so SaveChanges is a no-op
        // Changes are committed when CommitTransactionAsync is called
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _connection = _connectionFactory.GetOrCreateConnection(_connectionString);
        
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        _transaction = _connection.BeginTransaction();
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            _transaction.Commit();
        }
        catch
        {
            _transaction.Rollback();
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
            
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            _transaction.Rollback();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
            
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the Unit of Work and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the Unit of Work and releases resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Asynchronously disposes the Unit of Work and releases resources.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
