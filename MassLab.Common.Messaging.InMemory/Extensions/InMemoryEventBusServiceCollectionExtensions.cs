using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MassLab.Common.Messaging.Abstractions;
using MassLab.Common.Messaging.Extensions;

namespace MassLab.Common.Messaging.InMemory.Extensions;

/// <summary>
/// Service-collection extension to register the in-memory event bus.
/// </summary>
public static class InMemoryEventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="InMemoryEventBus"/> as <see cref="IEventBus"/>
    /// (singleton) and <see cref="InMemoryEventBusBackgroundService"/> as
    /// the channel reader. Also registers the core dispatcher.
    /// </summary>
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        services.AddMassLabMessagingCore();
        services.TryAddSingleton<InMemoryEventBus>();
        services.TryAddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
        services.AddHostedService<InMemoryEventBusBackgroundService>();
        return services;
    }
}
