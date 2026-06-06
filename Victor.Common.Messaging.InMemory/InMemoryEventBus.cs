using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Victor.Common.Messaging.Abstractions;
using Victor.Common.Messaging.Dispatch;

namespace Victor.Common.Messaging.InMemory;

/// <summary>
/// In-process <see cref="IEventBus"/> backed by an unbounded
/// <see cref="System.Threading.Channels.Channel{T}"/>. Suitable for local
/// development, tests, and small workloads inside a single process.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly Channel<IIntegrationEvent> _channel = Channel.CreateUnbounded<IIntegrationEvent>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    /// <summary>The channel reader, consumed by <see cref="InMemoryEventBusBackgroundService"/>.</summary>
    internal ChannelReader<IIntegrationEvent> Reader => _channel.Reader;

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
        => PublishAsync((IIntegrationEvent)integrationEvent, cancellationToken);

    /// <inheritdoc />
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (integrationEvent is null) throw new ArgumentNullException(nameof(integrationEvent));
        await _channel.Writer.WriteAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
    }

    internal void Complete() => _channel.Writer.TryComplete();
}

/// <summary>
/// Drains the in-memory bus channel and dispatches each event to the
/// registered handlers via <see cref="IIntegrationEventDispatcher"/>.
/// </summary>
public class InMemoryEventBusBackgroundService : BackgroundService
{
    private readonly InMemoryEventBus _bus;
    private readonly IIntegrationEventDispatcher _dispatcher;
    private readonly ILogger<InMemoryEventBusBackgroundService> _logger;

    /// <summary>Initializes a new instance.</summary>
    public InMemoryEventBusBackgroundService(
        InMemoryEventBus bus,
        IIntegrationEventDispatcher dispatcher,
        ILogger<InMemoryEventBusBackgroundService> logger)
    {
        _bus = bus;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InMemoryEventBus background service started");
        try
        {
            await foreach (var evt in _bus.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await _dispatcher.DispatchAsync(evt, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler error dispatching {EventType} (id={EventId})",
                        evt.GetType().Name, evt.EventId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        finally
        {
            _logger.LogInformation("InMemoryEventBus background service stopping");
        }
    }

    /// <inheritdoc />
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _bus.Complete();
        return base.StopAsync(cancellationToken);
    }
}
