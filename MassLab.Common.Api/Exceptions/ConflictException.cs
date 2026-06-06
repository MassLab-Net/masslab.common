namespace MassLab.Common.Api.Exceptions;

/// <summary>
/// Exception thrown when a request conflicts with the current state of the resource.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConflictException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
