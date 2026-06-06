namespace MassLab.Common.Messaging.RabbitMQ.Configuration;

/// <summary>
/// RabbitMQ-specific options.
/// </summary>
public class RabbitMqOptions
{
    /// <summary>Configuration section name (<c>RabbitMq</c>).</summary>
    public const string SectionName = "RabbitMq";

    /// <summary>Broker host (e.g. <c>localhost</c> or <c>rabbitmq</c>).</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Broker port (default <c>5672</c>).</summary>
    public int Port { get; set; } = 5672;

    /// <summary>Username (default <c>guest</c>).</summary>
    public string UserName { get; set; } = "guest";

    /// <summary>Password (default <c>guest</c>).</summary>
    public string Password { get; set; } = "guest";

    /// <summary>vhost (default <c>/</c>).</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>Topic exchange name (default <c>masslab.events</c>).</summary>
    public string ExchangeName { get; set; } = "masslab.events";

    /// <summary>Queue name for inbound subscriptions (default <c>masslab.queue</c>).</summary>
    public string QueueName { get; set; } = "masslab.queue";

    /// <summary>If <c>true</c>, declares a dead-letter queue and exchange.</summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>Max delivery attempts before message goes to DLQ (default 5).</summary>
    public int MaxDeliveryAttempts { get; set; } = 5;
}
