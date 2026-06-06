using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Victor.Common.Messaging.Abstractions;
using Victor.Common.Messaging.Dispatch;

namespace Victor.Common.Messaging.Extensions;

/// <summary>
/// Service-collection extensions to register the messaging dispatcher and
/// scan assemblies for <see cref="IIntegrationEventHandler{TEvent}"/> impls.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IIntegrationEventDispatcher"/> with singleton lifetime.
    /// The dispatcher creates its own DI scope per dispatch call, so it is safe
    /// to inject into singleton hosted services (background consumers).
    /// </summary>
    public static IServiceCollection AddVictorMessagingCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        return services;
    }

    /// <summary>
    /// Scans the supplied assemblies for closed-generic implementations of
    /// <see cref="IIntegrationEventHandler{TEvent}"/> and registers them with
    /// scoped lifetime.
    /// </summary>
    public static IServiceCollection AddIntegrationEventHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes()
                         .Where(t => !t.IsAbstract && !t.IsInterface))
            {
                foreach (var iface in type.GetInterfaces()
                             .Where(i => i.IsGenericType
                                         && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
                {
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(iface, type));
                }
            }
        }
        return services;
    }
}
