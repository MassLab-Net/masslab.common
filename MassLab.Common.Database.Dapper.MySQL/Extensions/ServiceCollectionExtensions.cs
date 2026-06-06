using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MassLab.Common.Database.Abstractions;
using MassLab.Common.Database.Configuration;
using MassLab.Common.Database.Dapper.Abstractions;
using MassLab.Common.Database.Dapper.MySQL.Factories;
using MassLab.Common.Database.Dapper.Repositories;
using MassLab.Common.Database.Dapper.UnitOfWork;

namespace MassLab.Common.Database.Dapper.MySQL.Extensions;

/// <summary>
/// Extension methods for registering MySQL database services with Dapper.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MySQL Dapper services including connection factory, repositories, and unit of work.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configurationSection">The configuration section name containing database options. Defaults to "Database".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMySqlDapper(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = "Database")
    {
        // Bind DatabaseOptions from configuration
        services.Configure<DatabaseOptions>(configuration.GetSection(configurationSection));

        // Register MySQL ConnectionFactory
        services.AddSingleton<IConnectionFactory, MySqlConnectionFactory>();

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
