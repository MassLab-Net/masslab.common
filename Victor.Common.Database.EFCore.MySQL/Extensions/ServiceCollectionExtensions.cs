using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Victor.Common.Database.Abstractions;
using Victor.Common.Database.Configuration;
using Victor.Common.Database.EFCore.MySQL.Internal;
using Victor.Common.Database.EFCore.MySQL.Repositories;
using Victor.Common.Database.EFCore.UnitOfWork;

namespace Victor.Common.Database.EFCore.MySQL.Extensions;

/// <summary>
/// Extension methods for registering MySQL database services with Entity Framework Core.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MySQL Entity Framework Core services including DbContext, repositories, and unit of work.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configurationSection">The configuration section name containing database options. Defaults to "Database".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMySqlDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Database")
        where TContext : DbContext
    {
        // Bind DatabaseOptions from configuration
        services.Configure<DatabaseOptions>(configuration.GetSection(configurationSection));
        var options = new DatabaseOptions();
        configuration.GetSection(configurationSection).Bind(options);

        // Register DbContext with MySQL provider
        services.AddDbContext<TContext>(opts =>
            opts.UseMySql(options.WriteConnectionString, ServerVersion.AutoDetect(options.WriteConnectionString)));

        // Register repositories and UoW
        services.AddScoped(typeof(IReadRepository<>), typeof(MySqlReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(MySqlWriteRepository<>));
        services.AddScoped<IUnitOfWork, EFCoreUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Registers MySQL Entity Framework Core services with separate read and write DbContext instances.
    /// </summary>
    /// <typeparam name="TWriteContext">The write DbContext type.</typeparam>
    /// <typeparam name="TReadContext">The read DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configurationSection">The configuration section name containing database options. Defaults to "Database".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMySqlDbContextWithSeparateReadWrite<TWriteContext, TReadContext>(
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

        // Register write DbContext
        services.AddDbContext<TWriteContext>(opts =>
            opts.UseMySql(options.WriteConnectionString, ServerVersion.AutoDetect(options.WriteConnectionString)));

        // Register read DbContext
        services.AddDbContext<TReadContext>(opts =>
            opts.UseMySql(options.GetReadConnectionString(), ServerVersion.AutoDetect(options.GetReadConnectionString())));

        // Register repositories with appropriate contexts
        services.AddScoped(typeof(IWriteRepository<>), sp =>
        {
            var entityType = typeof(IWriteRepository<>).GetGenericArguments()[0];
            var factory = RepositoryFactory.CreateWriteRepositoryFactory<TWriteContext>();
            return factory(sp, entityType);
        });

        services.AddScoped(typeof(IReadRepository<>), sp =>
        {
            var entityType = typeof(IReadRepository<>).GetGenericArguments()[0];
            var factory = RepositoryFactory.CreateReadRepositoryFactory<TReadContext>();
            return factory(sp, entityType);
        });

        // Register UoW with write context
        services.AddScoped<IUnitOfWork>(sp =>
        {
            var writeContext = sp.GetRequiredService<TWriteContext>();
            return new EFCoreUnitOfWork(writeContext);
        });

        return services;
    }
}
