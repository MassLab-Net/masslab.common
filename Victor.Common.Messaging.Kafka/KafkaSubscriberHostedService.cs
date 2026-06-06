using System.Reflection;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Victor.Common.Messaging.Abstractions;
using Victor.Common.Messaging.Dispatch;
using Victor.Common.Messaging.Kafka.Configuration;

namespace Victor.Common.Messaging.Kafka;

/// <summary>
/// Background service that consumes from the configured Kafka topic and
/// dispatches each event to the registered handlers. Commits offsets only
/// after the handler succeeds (when <see cref="KafkaOptions.CommitAfterSuccess"/>
/// is <c>true</c>).
/// </summary>
public class KafkaSubscriberHostedService : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IIntegrationEventDispatcher _dispatcher;
    private readonly ILogger<KafkaSubscriberHostedService> _logger;
    private readonly Dictionary<string, Type> _eventTypes = new();

    /// <summary>Initializes a new instance.</summary>
    public KafkaSubscriberHostedService(
        IOptions<KafkaOptions> options,
        IIntegrationEventDispatcher dispatcher,
        ILogger<KafkaSubscriberHostedService> logger)
    {
        _options = options.Value;
        _dispatcher = dispatcher;
        _logger = logger;

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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(_options.AutoOffsetReset, true, out var reset)
                ? reset
                : Confluent.Kafka.AutoOffsetReset.Earliest,
            EnableAutoCommit = !_options.CommitAfterSuccess,
            ClientId = "victor-consumer",
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(cfg).Build();
        consumer.Subscribe(_options.Topic);

        _logger.LogInformation("Kafka subscriber started on topic {Topic} (group={GroupId})",
            _options.Topic, _options.GroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, byte[]>? result;
                try { result = consumer.Consume(stoppingToken); }
                catch (OperationCanceledException) { break; }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    continue;
                }

                if (result?.Message is null) continue;

                var typeHeader = result.Message.Headers?.FirstOrDefault(h => h.Key == "Type");
                var typeName = typeHeader is null ? null : Encoding.UTF8.GetString(typeHeader.GetValueBytes());

                if (typeName is null || !_eventTypes.TryGetValue(typeName, out var eventType))
                {
                    _logger.LogWarning("Unknown integration event type: {Type}", typeName);
                    if (_options.CommitAfterSuccess) consumer.Commit(result);
                    continue;
                }

                try
                {
                    var json = Encoding.UTF8.GetString(result.Message.Value);
                    if (JsonSerializer.Deserialize(json, eventType) is IIntegrationEvent evt)
                    {
                        await _dispatcher.DispatchAsync(evt, stoppingToken).ConfigureAwait(false);
                    }
                    if (_options.CommitAfterSuccess) consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    var succeeded = false;
                    for (int attempt = 2; attempt <= 3; attempt++)
                    {
                        try
                        {
                            var json = Encoding.UTF8.GetString(result.Message.Value);
                            if (JsonSerializer.Deserialize(json, eventType) is IIntegrationEvent evt)
                            {
                                await _dispatcher.DispatchAsync(evt, stoppingToken).ConfigureAwait(false);
                            }
                            succeeded = true;
                            break;
                        }
                        catch (Exception retryEx)
                        {
                            _logger.LogWarning(retryEx, "Retry {Attempt}/3 failed for {Type} (offset={Offset})", attempt, typeName, result.Offset.Value);
                        }
                    }

                    if (!succeeded)
                    {
                        _logger.LogError(ex, "All 3 retries exhausted for {Type} (offset={Offset}). Skipping poison message.", typeName, result.Offset.Value);
                        if (_options.CommitAfterSuccess) consumer.Commit(result);
                    }
                    else
                    {
                        if (_options.CommitAfterSuccess) consumer.Commit(result);
                    }
                }
            }
        }
        finally
        {
            try { consumer.Close(); } catch { /* ignore */ }
            _logger.LogInformation("Kafka subscriber stopped");
        }
    }
}
