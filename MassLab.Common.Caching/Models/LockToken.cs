namespace MassLab.Common.Caching.Models;

/// <summary>
/// Represents a distributed lock token.
/// </summary>
public sealed class LockToken
{
    /// <summary>
    /// Gets the lock key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the unique token value used to verify lock ownership.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// Gets the time when the lock was acquired.
    /// </summary>
    public DateTimeOffset AcquiredAt { get; }

    public LockToken(string key, string token)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Token = token ?? throw new ArgumentNullException(nameof(token));
        AcquiredAt = DateTimeOffset.UtcNow;
    }
}
