using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Victor.Common.Messaging.Abstractions;
using Victor.Common.Messaging.RabbitMQ.Configuration;

namespace Victor.Common.Messaging.RabbitMQ;

/// <summary>
/// Long-lived RabbitMQ connection. Owns one <see cref="IConnection"/> for
/// the application lifetime with automatic recovery and retry on connect.
/// </summary>
public sealed class RabbitMqConnection : IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly object _lock = new();
    private IConnection? _connection;

    /// <summary>Initializes a new instance.</summary>
    public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>The shared <see cref="IConnection"/> (lazy-initialized with retry).</summary>
    public IConnection Connection
    {
        get
        {
            if (_connection is { IsOpen: true }) return _connection;
            lock (_lock)
            {
                if (_connection is { IsOpen: true }) return _connection;
                _connection = CreateConnectionWithRetry();
                return _connection;
            }
        }
    }

    private IConnection CreateConnectionWithRetry()
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
        };

        var delayMs = 500;
        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation("Opening RabbitMQ connection to {Host}:{Port} (attempt {Attempt})", _options.Host, _options.Port, attempt);
                return factory.CreateConnection();
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "RabbitMQ connection attempt {Attempt} failed, retrying in {Delay}ms", attempt, delayMs);
                Thread.Sleep(delayMs);
                delayMs = Math.Min(delayMs * 2, 10000);
            }
        }
        // Final attempt — let it throw
        return factory.CreateConnection();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_connection is not null)
        {
            try { _connection.Close(); } catch { /* ignore */ }
            _connection.Dispose();
        }
    }
}

/// <summary>
/// RabbitMQ <see cref="IEventBus"/> implementation. Publishes to a topic
/// exchange using the event type's full name as the routing key.
/// </summary>
public class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private IModel? _channel;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Initializes a new instance.</summary>
    public RabbitMqEventBus(
        RabbitMqConnection connection,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqEventBus> logger)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    private IModel GetOrCreateChannel()
    {
        if (_channel is { IsOpen: true }) return _channel;
        _channel?.Dispose();
        _channel = _connection.Connection.CreateModel();
        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        return _channel;
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
        => PublishAsync((IIntegrationEvent)integrationEvent, cancellationToken);

    /// <inheritdoc />
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (integrationEvent is null) throw new ArgumentNullException(nameof(integrationEvent));

        await _channelLock.WaitAsync(cancellationToken);
        try
        {
            var channel = GetOrCreateChannel();

            var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType(), JsonOpts);
            var routingKey = integrationEvent.GetType().FullName ?? "unknown";

            var props = channel.CreateBasicProperties();
            props.MessageId = integrationEvent.EventId.ToString();
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            props.ContentType = "application/json";
            props.Type = routingKey;
            props.DeliveryMode = 2; // persistent

            // propagate W3C traceparent
            var traceparent = Activity.Current?.Id;
            if (!string.IsNullOrWhiteSpace(traceparent))
            {
                props.Headers = new Dictionary<string, object> { ["traceparent"] = Encoding.UTF8.GetBytes(traceparent) };
            }

            channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);

            _logger.LogDebug("Published {EventType} (id={EventId}) to {Exchange}",
                routingKey, integrationEvent.EventId, _options.ExchangeName);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _channel?.Dispose();
        _channelLock.Dispose();
    }
}
