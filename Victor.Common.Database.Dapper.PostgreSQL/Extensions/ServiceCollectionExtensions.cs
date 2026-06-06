using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Victor.Common.Database.Abstractions;
using Victor.Common.Database.Configuration;
using Victor.Common.Database.Dapper.Abstractions;
using Victor.Common.Database.Dapper.PostgreSQL.Factories;
using Victor.Common.Database.Dapper.Repositories;
using Victor.Common.Database.Dapper.UnitOfWork;

namespace Victor.Common.Database.Dapper.PostgreSQL.Extensions;

/// <summary>
/// Extension methods for registering PostgreSQL database services with Dapper.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers PostgreSQL Dapper services including connection factory and repositories.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configurationSection">The configuration section name containing database options. Defaults to "Database".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlDapper(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Database")
    {
        // Bind DatabaseOptions from configuration
        services.Configure<DatabaseOptions>(configuration.GetSection(configurationSection));

        // Register PostgreSQL ConnectionFactory
        services.AddSingleton<IConnectionFactory, PostgreSqlConnectionFactory>();

        // Register DapperReadRepository with read connection string
        services.AddScoped<IDapperReadRepository>(sp =>
        {
            var factory = sp.GetRequiredService<IConnectionFactory>();
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>();
            return new DapperReadRepository(factory, options.Value.GetReadConnectionString());
        });

        // Register DapperWriteRepository with write connection string
        services.AddScoped<IDapperWriteRepository>(sp =>
        {
            var factory = sp.GetRequiredService<IConnectionFactory>();
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>();
            return new DapperWriteRepository(factory, options.Value.WriteConnectionString);
        });

        // Register IUnitOfWork with write connection string
        services.AddScoped<IUnitOfWork>(sp =>
        {
            var factory = sp.GetRequiredService<IConnectionFactory>();
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>();
            return new DapperUnitOfWork(factory, options.Value.WriteConnectionString);
        });

        return services;
    }
}
