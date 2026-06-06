namespace Victor.Common.Caching.Exceptions;

/// <summary>
/// Exception thrown when a cache connection fails.
/// </summary>
public class CacheConnectionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheConnectionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CacheConnectionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheConnectionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CacheConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
