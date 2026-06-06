using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MassLab.Common.Messaging.Abstractions;
using MassLab.Common.Messaging.Extensions;
using MassLab.Common.Messaging.Kafka.Configuration;

namespace MassLab.Common.Messaging.Kafka.Extensions;

/// <summary>
/// Service-collection extensions to register the Kafka event bus.
/// </summary>
public static class KafkaEventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="KafkaEventBus"/> as <see cref="IEventBus"/> and
    /// optionally the background subscriber.
    /// </summary>
    public static IServiceCollection AddKafkaEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KafkaOptions.SectionName,
        bool addSubscriber = true)
    {
        var options = new KafkaOptions();
        configuration.GetSection(sectionName).Bind(options);
        Validate(options);

        services.Configure<KafkaOptions>(configuration.GetSection(sectionName));
        return AddKafkaEventBusCore(services, addSubscriber);
    }

    /// <summary>Same as above using an in-line configurator.</summary>
    public static IServiceCollection AddKafkaEventBus(
        this IServiceCollection services,
        Action<KafkaOptions> configure,
        bool addSubscriber = true)
    {
        var options = new KafkaOptions();
        configure(options);
        Validate(options);

        services.Configure(configure);
        return AddKafkaEventBusCore(services, addSubscriber);
    }

    private static IServiceCollection AddKafkaEventBusCore(IServiceCollection services, bool addSubscriber)
    {
        services.AddMassLabMessagingCore();
        services.TryAddSingleton<KafkaEventBus>();
        services.TryAddSingleton<IEventBus>(sp => sp.GetRequiredService<KafkaEventBus>());

        if (addSubscriber)
            services.AddHostedService<KafkaSubscriberHostedService>();

        return services;
    }

    private static void Validate(KafkaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
            throw new ArgumentException("Kafka bootstrap servers are required.", nameof(options.BootstrapServers));
        if (string.IsNullOrWhiteSpace(options.Topic))
            throw new ArgumentException("Kafka topic is required.", nameof(options.Topic));
        if (string.IsNullOrWhiteSpace(options.GroupId))
            throw new ArgumentException("Kafka group id is required.", nameof(options.GroupId));
        if (!string.Equals(options.Acks, "all", StringComparison.OrdinalIgnoreCase)
            && options.Acks is not "0" and not "1")
            throw new ArgumentException("Kafka acks must be 'all', '0', or '1'.", nameof(options.Acks));
        if (!string.Equals(options.AutoOffsetReset, "Earliest", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(options.AutoOffsetReset, "Latest", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Kafka auto offset reset must be 'Earliest' or 'Latest'.", nameof(options.AutoOffsetReset));
    }
}
