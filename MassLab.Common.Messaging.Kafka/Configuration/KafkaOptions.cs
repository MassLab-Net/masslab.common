namespace MassLab.Common.Messaging.Kafka.Configuration;

/// <summary>
/// Apache Kafka-specific options.
/// </summary>
public class KafkaOptions
{
    /// <summary>Configuration section name (<c>Kafka</c>).</summary>
    public const string SectionName = "Kafka";

    /// <summary>Bootstrap servers (e.g. <c>localhost:9092,host2:9092</c>).</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>Topic name to publish/consume.</summary>
    public string Topic { get; set; } = "masslab.events";

    /// <summary>Consumer group id (default <c>masslab-consumer</c>).</summary>
    public string GroupId { get; set; } = "masslab-consumer";

    /// <summary>Auto-offset-reset strategy (<c>Earliest</c> [default] / <c>Latest</c>).</summary>
    public string AutoOffsetReset { get; set; } = "Earliest";

    /// <summary>Enable Kafka idempotent producer (default <c>true</c>).</summary>
    public bool EnableIdempotence { get; set; } = true;

    /// <summary>Acks ('all' [default], '0', '1').</summary>
    public string Acks { get; set; } = "all";

    /// <summary>Whether the subscriber commits after successful handler dispatch (default <c>true</c>).</summary>
    public bool CommitAfterSuccess { get; set; } = true;
}
