using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Victor.Common.Database.EFCore.Interceptors;
using Victor.Common.Domain.Events;

namespace Victor.Common.Database.EFCore.Extensions;

/// <summary>
/// Helpers to register the audit / soft-delete / domain-event interceptors.
/// All are opt-in.
/// </summary>
public static class InterceptorServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AuditingSaveChangesInterceptor"/>. Provide a
    /// delegate that resolves the current user identifier from the request
    /// scope (e.g. <c>sp =&gt; sp.GetService&lt;ICurrentUser&gt;()?.UserId.ToString()</c>).
    /// </summary>
    public static IServiceCollection AddAuditingInterceptor(
        this IServiceCollection services,
        Func<IServiceProvider, string?>? currentUserResolver = null,
        Func<DateTime>? clock = null)
    {
        services.AddScoped<ISaveChangesInterceptor>(sp =>
            new AuditingSaveChangesInterceptor(
                () => currentUserResolver?.Invoke(sp),
                clock));
        return services;
    }

    /// <summary>Registers <see cref="SoftDeleteSaveChangesInterceptor"/>.</summary>
    public static IServiceCollection AddSoftDeleteInterceptor(
        this IServiceCollection services,
        Func<DateTime>? clock = null)
    {
        services.AddScoped<ISaveChangesInterceptor>(_ => new SoftDeleteSaveChangesInterceptor(clock));
        return services;
    }

    /// <summary>
    /// Registers <see cref="DomainEventDispatchInterceptor"/>. Supply a
    /// dispatcher that publishes events (typically via MediatR or
    /// <c>IEventBus</c>).
    /// </summary>
    public static IServiceCollection AddDomainEventDispatchInterceptor(
        this IServiceCollection services,
        Func<IServiceProvider, Func<IDomainEvent, CancellationToken, Task>> dispatcherFactory)
    {
        services.AddScoped<ISaveChangesInterceptor>(sp =>
            new DomainEventDispatchInterceptor(dispatcherFactory(sp)));
        return services;
    }
}
