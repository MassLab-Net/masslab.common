using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MassLab.Common.Database.Abstractions;

namespace MassLab.Common.Database.EFCore.UnitOfWork;

/// <summary>
/// Entity Framework Core implementation of Unit of Work pattern.
/// Manages database transactions and change tracking.
/// </summary>
public class EFCoreUnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreUnitOfWork"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EFCoreUnitOfWork(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
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
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Asynchronously disposes the Unit of Work and releases resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
