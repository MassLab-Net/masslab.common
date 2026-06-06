namespace Victor.Common.Caching.Exceptions;

/// <summary>
/// Exception thrown when serialization or deserialization fails.
/// </summary>
public class CacheSerializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheSerializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CacheSerializationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheSerializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CacheSerializationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
