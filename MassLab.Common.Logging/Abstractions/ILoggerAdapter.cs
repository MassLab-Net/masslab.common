namespace MassLab.Common.Logging.Abstractions;

/// <summary>
/// Provides a consistent logging interface across microservices.
/// </summary>
/// <typeparam name="T">The type whose name is used for the logger category name.</typeparam>
public interface ILoggerAdapter<T>
{
    /// <summary>
    /// Logs a trace message.
    /// </summary>
    void LogTrace(string message, params object[] args);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message template.</param>
    void LogInformation(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message template.</param>
    void LogWarning(string message, params object[] args);

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message template.</param>
    void LogError(Exception exception, string message, params object[] args);

    /// <summary>
    /// Logs a critical message with an exception.
    /// </summary>
    void LogCritical(Exception exception, string message, params object[] args);

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message template.</param>
    void LogDebug(string message, params object[] args);

    /// <summary>
    /// Checks if the given log level is enabled.
    /// </summary>
    bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel);

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <param name="state">The identifier for the scope.</param>
    /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
    IDisposable? BeginScope<TState>(TState state)
        where TState : notnull;
}
