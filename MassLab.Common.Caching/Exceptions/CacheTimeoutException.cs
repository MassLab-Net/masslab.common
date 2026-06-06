namespace MassLab.Common.Caching.Exceptions;

/// <summary>
/// Exception thrown when a cache operation times out.
/// </summary>
public class CacheTimeoutException : Exception
{
    /// <summary>
    /// Gets the name of the operation that timed out.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Gets the timeout duration.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheTimeoutException"/> class.
    /// </summary>
    /// <param name="operationName">The name of the operation that timed out.</param>
    /// <param name="timeout">The timeout duration.</param>
    public CacheTimeoutException(string operationName, TimeSpan timeout)
        : base($"Cache operation '{operationName}' timed out after {timeout.TotalSeconds} seconds.")
    {
        OperationName = operationName;
        Timeout = timeout;
    }
}
