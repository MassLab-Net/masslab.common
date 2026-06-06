using Microsoft.Extensions.Logging;
using MassLab.Common.Logging.Abstractions;

namespace MassLab.Common.Logging.Implementations;

/// <summary>
/// Wrapper around ILogger<T> that provides a consistent logging interface.
/// </summary>
/// <typeparam name="T">The type whose name is used for the logger category name.</typeparam>
public class LoggerAdapter<T> : ILoggerAdapter<T>
{
    private readonly ILogger<T> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerAdapter{T}"/> class.
    /// </summary>
    /// <param name="logger">The underlying ILogger instance.</param>
    public LoggerAdapter(ILogger<T> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void LogTrace(string message, params object[] args)
    {
        _logger.LogTrace(message, args);
    }

    /// <inheritdoc/>
    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    /// <inheritdoc/>
    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    /// <inheritdoc/>
    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }

    /// <inheritdoc/>
    public void LogCritical(Exception exception, string message, params object[] args)
    {
        _logger.LogCritical(exception, message, args);
    }

    /// <inheritdoc/>
    public void LogDebug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return _logger.BeginScope(state);
    }
}
