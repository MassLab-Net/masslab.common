using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Victor.Common.Database.Abstractions;
using Victor.Common.Database.Configuration;
using Victor.Common.Database.EFCore.Repositories;
using Victor.Common.Database.EFCore.UnitOfWork;

namespace Victor.Common.Database.EFCore.PostgreSQL.Extensions;

/// <summary>
/// Extension methods for registering PostgreSQL database services with Entity Framework Core.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a PostgreSQL DbContext with Entity Framework Core and related repository services.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configurationSection">The configuration section name containing database options. Defaults to "Database".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Database")
        where TContext : DbContext
    {
        // Bind DatabaseOptions from configuration
        services.Configure<DatabaseOptions>(configuration.GetSection(configurationSection));
        var options = new DatabaseOptions();
        configuration.GetSection(configurationSection).Bind(options);

        // Register DbContext with PostgreSQL provider
        services.AddDbContext<TContext>(opts =>
            opts.UseNpgsql(options.WriteConnectionString));

        // Register DbContext as base DbContext for repositories
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

        // Register repositories and unit of work
        services.AddScoped(typeof(IReadRepository<>), typeof(EFCoreReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(EFCoreWriteRepository<>));
        services.AddScoped<IUnitOfWork, EFCoreUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Registers separate PostgreSQL DbContexts for read and write operations with Entity Framework Core.
    /// Note: Due to .NET DI limitations with open generics, repositories will use the write context by default.
    /// For true read/write separation, create custom repositories in your infrastructure layer.
    /// </summary>
    /// <typeparam name="TWriteContext">The DbContext type for write operations.</typeparam>
    /// <typeparam name="TReadContext">The DbContext type for read operations.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configurationSection">The configuration section name containing database options. Defaults to "Database".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlDbContextWithSeparateReadWrite<TWriteContext, TReadContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Database")
        where TWriteContext : DbContext
        where TReadContext : DbContext
    {
        // Bind DatabaseOptions from configuration
        services.Configure<DatabaseOptions>(configuration.GetSection(configurationSection));
        var options = new DatabaseOptions();
        configuration.GetSection(configurationSection).Bind(options);

        // Register write DbContext with PostgreSQL provider
        services.AddDbContext<TWriteContext>(opts =>
            opts.UseNpgsql(options.WriteConnectionString));

        // Register read DbContext with PostgreSQL provider
        services.AddDbContext<TReadContext>(opts =>
            opts.UseNpgsql(options.GetReadConnectionString()));

        // Register write context as the default DbContext for repositories
        // This allows IReadRepository and IWriteRepository to work with the write context
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TWriteContext>());

        // Register repositories using the default DbContext (write context)
        services.AddScoped(typeof(IReadRepository<>), typeof(EFCoreReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(EFCoreWriteRepository<>));

        // Register IUnitOfWork with write context
        services.AddScoped<IUnitOfWork, EFCoreUnitOfWork>();

        return services;
    }
}
