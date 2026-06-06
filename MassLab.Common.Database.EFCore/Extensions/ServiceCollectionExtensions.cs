using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MassLab.Common.Database.Abstractions;
using MassLab.Common.Database.Configuration;
using MassLab.Common.Database.EFCore.Repositories;
using MassLab.Common.Database.EFCore.UnitOfWork;

namespace MassLab.Common.Database.EFCore.Extensions;

/// <summary>
/// Extension methods for registering EF Core repositories and Unit of Work with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core repositories and Unit of Work with the service collection.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configurationSection">The configuration section name for database options. Defaults to "Database".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEFCoreRepositories<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Database")
        where TContext : DbContext
    {
        // Bind database options from configuration
        var databaseOptions = new DatabaseOptions();
        configuration.GetSection(configurationSection).Bind(databaseOptions);

        // Register DatabaseOptions
        services.Configure<DatabaseOptions>(configuration.GetSection(configurationSection));

        // Register DbContext with write connection string
        services.AddDbContext<TContext>(options =>
        {
            // The actual database provider (SQL Server, PostgreSQL, etc.) should be configured
            // by the consuming application. This method only registers the context.
        }, ServiceLifetime.Scoped);

        // Register DbContext as the base DbContext type for repositories
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<TContext>());

        // Register repositories with scoped lifetime
        services.AddScoped(typeof(IReadRepository<>), typeof(EFCoreReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(EFCoreWriteRepository<>));

        // Register Unit of Work with scoped lifetime
        services.AddScoped<IUnitOfWork, EFCoreUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Registers EF Core repositories and Unit of Work with the service collection using a custom DbContext configuration action.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">The action to configure DbContextOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEFCoreRepositories<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TContext : DbContext
    {
        // Register DbContext with custom configuration
        services.AddDbContext<TContext>(optionsAction, ServiceLifetime.Scoped);

        // Register DbContext as the base DbContext type for repositories
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<TContext>());

        // Register repositories with scoped lifetime
        services.AddScoped(typeof(IReadRepository<>), typeof(EFCoreReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(EFCoreWriteRepository<>));

        // Register Unit of Work with scoped lifetime
        services.AddScoped<IUnitOfWork, EFCoreUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Registers EF Core repositories and Unit of Work with the service collection using separate read and write DbContexts.
    /// </summary>
    /// <typeparam name="TWriteContext">The write DbContext type.</typeparam>
    /// <typeparam name="TReadContext">The read DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="writeOptionsAction">The action to configure write DbContextOptions.</param>
    /// <param name="readOptionsAction">The action to configure read DbContextOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEFCoreRepositoriesWithSeparateReadWrite<TWriteContext, TReadContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> writeOptionsAction,
        Action<DbContextOptionsBuilder> readOptionsAction)
        where TWriteContext : DbContext
        where TReadContext : DbContext
    {
        // Register write DbContext
        services.AddDbContext<TWriteContext>(writeOptionsAction, ServiceLifetime.Scoped);

        // Register read DbContext
        services.AddDbContext<TReadContext>(readOptionsAction, ServiceLifetime.Scoped);

        // Register the read context as the default DbContext for open-generic IReadRepository<>
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TReadContext>());
        services.AddScoped(typeof(IReadRepository<>), typeof(EFCoreReadRepository<>));

        // Register write repository — uses the write context via a typed wrapper
        services.AddScoped(typeof(IWriteRepository<>), typeof(EFCoreWriteRepository<>));

        // Override DbContext resolution for write operations: UnitOfWork uses write context
        services.AddScoped<IUnitOfWork>(sp =>
            new EFCoreUnitOfWork(sp.GetRequiredService<TWriteContext>()));

        return services;
    }
}
