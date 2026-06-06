using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Victor.Common.Messaging.Abstractions;
using Victor.Common.Messaging.Dispatch;
using Victor.Common.Messaging.RabbitMQ.Configuration;

namespace Victor.Common.Messaging.RabbitMQ;

/// <summary>
/// Background service that consumes integration events from the configured
/// RabbitMQ queue and dispatches them to in-process handlers.
/// </summary>
public class RabbitMqSubscriberHostedService : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly IIntegrationEventDispatcher _dispatcher;
    private readonly ILogger<RabbitMqSubscriberHostedService> _logger;
    private readonly Dictionary<string, Type> _eventTypes = new();

    private IModel? _channel;

    /// <summary>Initializes a new instance.</summary>
    public RabbitMqSubscriberHostedService(
        RabbitMqConnection connection,
        IOptions<RabbitMqOptions> options,
        IIntegrationEventDispatcher dispatcher,
        ILogger<RabbitMqSubscriberHostedService> logger)
    {
        _connection = connection;
        _options = options.Value;
        _dispatcher = dispatcher;
        _logger = logger;

        // Discover IIntegrationEvent types in app domain assemblies (best effort).
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t is not null).ToArray()!; }
            catch { continue; }

            foreach (var t in types)
            {
                if (t is null || t.IsAbstract || t.IsInterface) continue;
                if (typeof(IIntegrationEvent).IsAssignableFrom(t) && t.FullName is { } name)
                    _eventTypes[name] = t;
            }
        }
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.Connection.CreateModel();

        // exchange + DLQ topology
        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        var queueArgs = new Dictionary<string, object>();
        if (_options.EnableDeadLetterQueue)
        {
            var dlxName = _options.ExchangeName + ".dlx";
            var dlqName = _options.QueueName + ".dlq";
            _channel.ExchangeDeclare(dlxName, ExchangeType.Fanout, durable: true);
            _channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(dlqName, dlxName, routingKey: "");
            queueArgs["x-dead-letter-exchange"] = dlxName;
        }

        _channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind(_options.QueueName, _options.ExchangeName, routingKey: "#");
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 16, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var typeName = ea.BasicProperties?.Type ?? ea.RoutingKey;
                if (!_eventTypes.TryGetValue(typeName, out var eventType))
                {
                    _logger.LogWarning("Unknown integration event type: {Type}", typeName);
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                var json = Encoding.UTF8.GetString(ea.Body.Span);
                if (JsonSerializer.Deserialize(json, eventType) is not IIntegrationEvent evt)
                {
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await _dispatcher.DispatchAsync(evt, stoppingToken).ConfigureAwait(false);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming RabbitMQ message");
                // do not requeue — let RabbitMQ apply retry / DLQ semantics
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(_options.QueueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("RabbitMQ subscriber started on queue {Queue} bound to {Exchange}",
            _options.QueueName, _options.ExchangeName);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
