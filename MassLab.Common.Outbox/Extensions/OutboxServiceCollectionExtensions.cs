using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MassLab.Common.Outbox.Configuration;
using MassLab.Common.Outbox.Interceptors;

namespace MassLab.Common.Outbox.Extensions;

/// <summary>
/// Service-collection extensions for the transactional outbox.
/// </summary>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="OutboxSaveChangesInterceptor"/> (capture
    /// domain events into outbox) and the <see cref="OutboxBackgroundService{TDbContext}"/>
    /// (poll + dispatch).
    /// </summary>
    public static IServiceCollection AddOutbox<TDbContext>(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = OutboxOptions.SectionName)
        where TDbContext : DbContext
    {
        var options = new OutboxOptions();
        configuration?.GetSection(sectionName).Bind(options);
        Validate(options);

        if (configuration != null)
            services.Configure<OutboxOptions>(configuration.GetSection(sectionName));
        else
            services.Configure<OutboxOptions>(_ => { });

        services.TryAddEnumerable(ServiceDescriptor
            .Scoped<ISaveChangesInterceptor, OutboxSaveChangesInterceptor>());

        services.AddHostedService<OutboxBackgroundService<TDbContext>>();

        return services;
    }

    /// <summary>Same as above using an in-line configurator.</summary>
    public static IServiceCollection AddOutbox<TDbContext>(
        this IServiceCollection services,
        Action<OutboxOptions> configure)
        where TDbContext : DbContext
    {
        var options = new OutboxOptions();
        configure(options);
        Validate(options);

        services.Configure(configure);
        services.TryAddEnumerable(ServiceDescriptor
            .Scoped<ISaveChangesInterceptor, OutboxSaveChangesInterceptor>());
        services.AddHostedService<OutboxBackgroundService<TDbContext>>();
        return services;
    }

    private static void Validate(OutboxOptions options)
    {
        if (options.PollingInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options.PollingInterval), options.PollingInterval, "Polling interval must be greater than zero.");
        if (options.BatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.BatchSize), options.BatchSize, "Batch size must be greater than zero.");
        if (options.MaxAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaxAttempts), options.MaxAttempts, "Max attempts must be greater than zero.");
        if (options.RetentionDays < 0)
            throw new ArgumentOutOfRangeException(nameof(options.RetentionDays), options.RetentionDays, "Retention days cannot be negative.");
    }
}
