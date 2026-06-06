namespace MassLab.Common.Api.Exceptions;

/// <summary>
/// Exception thrown when authentication is required but not provided or invalid.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UnauthorizedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
