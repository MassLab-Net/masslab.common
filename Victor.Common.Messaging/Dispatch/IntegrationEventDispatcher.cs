using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Victor.Common.Messaging.Abstractions;

namespace Victor.Common.Messaging.Dispatch;

/// <summary>
/// Resolves all <see cref="IIntegrationEventHandler{TEvent}"/> instances for
/// an event from DI and invokes them in order.
/// </summary>
public interface IIntegrationEventDispatcher
{
    /// <summary>Dispatches the integration event to all registered handlers.</summary>
    Task DispatchAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class IntegrationEventDispatcher : IIntegrationEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationEventDispatcher> _logger;
    private static readonly ConcurrentDictionary<Type, MethodInfo> MethodCache = new();

    /// <summary>Initializes a new instance.</summary>
    public IntegrationEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<IntegrationEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (integrationEvent is null) throw new ArgumentNullException(nameof(integrationEvent));

        var eventType = integrationEvent.GetType();
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType).Cast<object>().ToList();

        if (handlers.Count == 0)
        {
            _logger.LogDebug("No handlers registered for {EventType} (id={EventId})",
                eventType.Name, integrationEvent.EventId);
            return;
        }

        var method = MethodCache.GetOrAdd(eventType, _ =>
            handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!);

        foreach (var handler in handlers)
        {
            try
            {
                var task = (Task)method.Invoke(handler, new object[] { integrationEvent, cancellationToken })!;
                await task.ConfigureAwait(false);
                _logger.LogDebug("Dispatched {EventType} (id={EventId}) to {Handler}",
                    eventType.Name, integrationEvent.EventId, handler.GetType().Name);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                _logger.LogError(ex.InnerException, "Handler {Handler} failed for {EventType} (id={EventId})",
                    handler.GetType().Name, eventType.Name, integrationEvent.EventId);
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {Handler} failed for {EventType} (id={EventId})",
                    handler.GetType().Name, eventType.Name, integrationEvent.EventId);
                throw;
            }
        }
    }
}
