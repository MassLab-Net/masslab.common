using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Victor.Common.Messaging.Abstractions;
using Victor.Common.Messaging.Kafka.Configuration;

namespace Victor.Common.Messaging.Kafka;

/// <summary>
/// Kafka <see cref="IEventBus"/>. Uses the event type's full name as the
/// <c>Type</c> message header and <see cref="IIntegrationEvent.EventId"/>
/// as the partition key.
/// </summary>
public class KafkaEventBus : IEventBus, IDisposable
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaEventBus> _logger;
    private readonly Lazy<IProducer<string, byte[]>> _producer;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Initializes a new instance.</summary>
    public KafkaEventBus(IOptions<KafkaOptions> options, ILogger<KafkaEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;
        _producer = new Lazy<IProducer<string, byte[]>>(BuildProducer, isThreadSafe: true);
    }

    private IProducer<string, byte[]> BuildProducer()
    {
        var cfg = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            EnableIdempotence = _options.EnableIdempotence,
            Acks = _options.Acks switch { "0" => Acks.None, "1" => Acks.Leader, _ => Acks.All },
            ClientId = "victor-producer",
        };
        return new ProducerBuilder<string, byte[]>(cfg).Build();
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
        => PublishAsync((IIntegrationEvent)integrationEvent, cancellationToken);

    /// <inheritdoc />
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (integrationEvent is null) throw new ArgumentNullException(nameof(integrationEvent));

        var typeName = integrationEvent.GetType().FullName ?? "unknown";
        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType(), JsonOpts);

        var headers = new Headers
        {
            { "Type",       Encoding.UTF8.GetBytes(typeName) },
            { "EventId",    Encoding.UTF8.GetBytes(integrationEvent.EventId.ToString()) },
            { "OccurredOn", Encoding.UTF8.GetBytes(integrationEvent.OccurredOn.ToString("O")) },
        };
        var traceparent = Activity.Current?.Id;
        if (!string.IsNullOrWhiteSpace(traceparent))
            headers.Add("traceparent", Encoding.UTF8.GetBytes(traceparent));

        var msg = new Message<string, byte[]>
        {
            Key = integrationEvent.EventId.ToString(),
            Value = body,
            Headers = headers,
        };

        var result = await _producer.Value.ProduceAsync(_options.Topic, msg, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Published {Type} (id={EventId}) to {Topic} partition={Partition} offset={Offset}",
            typeName, integrationEvent.EventId, result.Topic, result.Partition.Value, result.Offset.Value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_producer.IsValueCreated)
        {
            _producer.Value.Flush(TimeSpan.FromSeconds(5));
            _producer.Value.Dispose();
        }
    }
}
