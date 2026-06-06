using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Victor.Common.Messaging.Abstractions;
using Victor.Common.Messaging.Extensions;
using Victor.Common.Messaging.RabbitMQ.Configuration;

namespace Victor.Common.Messaging.RabbitMQ.Extensions;

/// <summary>
/// Service-collection extensions to register the RabbitMQ event bus.
/// </summary>
public static class RabbitMqEventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="RabbitMqEventBus"/> as <see cref="IEventBus"/>
    /// (singleton), the long-lived <see cref="RabbitMqConnection"/>, and the
    /// background subscriber that drains the configured queue.
    /// </summary>
    public static IServiceCollection AddRabbitMqEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = RabbitMqOptions.SectionName,
        bool addSubscriber = true)
    {
        var options = new RabbitMqOptions();
        configuration.GetSection(sectionName).Bind(options);
        Validate(options);

        services.Configure<RabbitMqOptions>(configuration.GetSection(sectionName));
        return AddRabbitMqEventBusCore(services, addSubscriber);
    }

    /// <summary>
    /// Same as <see cref="AddRabbitMqEventBus(IServiceCollection, IConfiguration, string, bool)"/>
    /// but using an in-line configurator.
    /// </summary>
    public static IServiceCollection AddRabbitMqEventBus(
        this IServiceCollection services,
        Action<RabbitMqOptions> configure,
        bool addSubscriber = true)
    {
        var options = new RabbitMqOptions();
        configure(options);
        Validate(options);

        services.Configure(configure);
        return AddRabbitMqEventBusCore(services, addSubscriber);
    }

    private static IServiceCollection AddRabbitMqEventBusCore(IServiceCollection services, bool addSubscriber)
    {
        services.AddVictorMessagingCore();
        services.TryAddSingleton<RabbitMqConnection>();
        services.TryAddSingleton<RabbitMqEventBus>();
        services.TryAddSingleton<IEventBus>(sp => sp.GetRequiredService<RabbitMqEventBus>());

        if (addSubscriber)
            services.AddHostedService<RabbitMqSubscriberHostedService>();

        return services;
    }

    private static void Validate(RabbitMqOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
            throw new ArgumentException("RabbitMQ host is required.", nameof(options.Host));
        if (options.Port <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.Port), options.Port, "RabbitMQ port must be greater than zero.");
        if (string.IsNullOrWhiteSpace(options.UserName))
            throw new ArgumentException("RabbitMQ username is required.", nameof(options.UserName));
        if (string.IsNullOrWhiteSpace(options.VirtualHost))
            throw new ArgumentException("RabbitMQ virtual host is required.", nameof(options.VirtualHost));
        if (string.IsNullOrWhiteSpace(options.ExchangeName))
            throw new ArgumentException("RabbitMQ exchange name is required.", nameof(options.ExchangeName));
        if (string.IsNullOrWhiteSpace(options.QueueName))
            throw new ArgumentException("RabbitMQ queue name is required.", nameof(options.QueueName));
        if (options.MaxDeliveryAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaxDeliveryAttempts), options.MaxDeliveryAttempts, "RabbitMQ max delivery attempts must be greater than zero.");
    }
}
