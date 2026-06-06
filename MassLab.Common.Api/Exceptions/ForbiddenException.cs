namespace MassLab.Common.Api.Exceptions;

/// <summary>
/// Exception thrown when the user is authenticated but lacks permission to access a resource.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ForbiddenException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
