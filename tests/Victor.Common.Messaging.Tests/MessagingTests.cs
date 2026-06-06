using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Victor.Common.Messaging.Abstractions;
using Victor.Common.Messaging.Dispatch;
using Victor.Common.Messaging.Extensions;
using Victor.Common.Messaging.Kafka.Extensions;
using Victor.Common.Messaging.RabbitMQ.Extensions;

namespace Victor.Common.Messaging.Tests;

public class MessagingTests
{
    [Fact]
    public async Task Dispatcher_invokes_registered_handler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestHandler>();
        services.AddSingleton<IIntegrationEventHandler<TestEvent>>(sp => sp.GetRequiredService<TestHandler>());
        await using var provider = services.BuildServiceProvider();
        var dispatcher = new IntegrationEventDispatcher(provider, NullLogger<IntegrationEventDispatcher>.Instance);

        await dispatcher.DispatchAsync(new TestEvent());

        provider.GetRequiredService<TestHandler>().Handled.Should().BeTrue();
    }

    [Fact]
    public async Task Dispatcher_rethrows_handler_exception_without_reflection_wrapper()
    {
        var services = new ServiceCollection();
        services.AddScoped<IIntegrationEventHandler<TestEvent>, ThrowingHandler>();
        await using var provider = services.BuildServiceProvider();
        var dispatcher = new IntegrationEventDispatcher(provider, NullLogger<IntegrationEventDispatcher>.Instance);

        var act = () => dispatcher.DispatchAsync(new TestEvent());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("handler failed");
    }

    [Fact]
    public void AddIntegrationEventHandlers_is_idempotent_for_same_assembly()
    {
        var services = new ServiceCollection();

        services.AddIntegrationEventHandlers(typeof(MessagingTests).Assembly);
        services.AddIntegrationEventHandlers(typeof(MessagingTests).Assembly);

        services.Count(d => d.ServiceType == typeof(IIntegrationEventHandler<TestEvent>)
                            && d.ImplementationType == typeof(TestHandler))
            .Should().Be(1);
    }

    [Fact]
    public void Kafka_registration_rejects_invalid_options()
    {
        var services = new ServiceCollection();

        var act = () => services.AddKafkaEventBus(options => options.Topic = "", addSubscriber: false);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("Topic");
    }

    [Fact]
    public void Rabbit_registration_rejects_invalid_options()
    {
        var services = new ServiceCollection();

        var act = () => services.AddRabbitMqEventBus(options => options.Port = 0, addSubscriber: false);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Port");
    }

    [Fact]
    public void Kafka_registration_from_configuration_registers_event_bus_without_connecting()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topic"] = "events",
                ["Kafka:GroupId"] = "tests"
            })
            .Build();

        services.AddLogging();
        services.AddKafkaEventBus(configuration, addSubscriber: false);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IEventBus>().Should().NotBeNull();
    }

    private sealed record TestEvent : IntegrationEvent;

    private sealed class TestHandler : IIntegrationEventHandler<TestEvent>
    {
        public bool Handled { get; private set; }

        public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            Handled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IIntegrationEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("handler failed");
    }
}
