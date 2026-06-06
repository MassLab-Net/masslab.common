using Victor.Common.Caching.Models;

namespace Victor.Common.Caching.Abstractions;

/// <summary>
/// Defines the contract for distributed locking operations.
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// Attempts to acquire a distributed lock.
    /// </summary>
    /// <param name="key">The lock key.</param>
    /// <param name="timeout">Maximum time to wait for the lock.</param>
    /// <param name="expiration">Lock expiration time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A lock token if successful; otherwise, null.</returns>
    Task<LockToken?> AcquireLockAsync(
        string key, 
        TimeSpan timeout, 
        TimeSpan expiration, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a previously acquired lock.
    /// </summary>
    /// <param name="token">The lock token returned from AcquireLockAsync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the lock was released; otherwise, false.</returns>
    Task<bool> ReleaseLockAsync(LockToken token, CancellationToken cancellationToken = default);
}
