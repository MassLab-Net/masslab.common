namespace Victor.Common.Api.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
