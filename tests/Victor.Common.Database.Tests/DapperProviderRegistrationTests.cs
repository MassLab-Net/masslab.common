using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Victor.Common.Database.Abstractions;
using Victor.Common.Database.Dapper.Abstractions;
using Victor.Common.Database.Dapper.MySQL.Extensions;
using Victor.Common.Database.Dapper.PostgreSQL.Extensions;
using Victor.Common.Database.Dapper.SqlServer.Extensions;

namespace Victor.Common.Database.Tests;

public class DapperProviderRegistrationTests
{
    [Fact]
    public void AddPostgreSqlDapper_RegistersRawSqlRepositoriesAndUnitOfWork()
    {
        var services = new ServiceCollection();

        services.AddPostgreSqlDapper(CreateConfiguration("Host=localhost;Database=write", "Host=localhost;Database=read"));

        AssertDapperServicesResolve(services);
    }

    [Fact]
    public void AddSqlServerDapper_RegistersRawSqlRepositoriesAndUnitOfWork()
    {
        var services = new ServiceCollection();

        services.AddSqlServerDapper(CreateConfiguration("Server=localhost;Database=write", "Server=localhost;Database=read"));

        AssertDapperServicesResolve(services);
    }

    [Fact]
    public void AddMySqlDapper_RegistersRawSqlRepositoriesAndUnitOfWork()
    {
        var services = new ServiceCollection();

        services.AddMySqlDapper(CreateConfiguration("Server=localhost;Database=write", "Server=localhost;Database=read"));

        AssertDapperServicesResolve(services);
    }

    private static IConfiguration CreateConfiguration(string writeConnectionString, string readConnectionString)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:WriteConnectionString"] = writeConnectionString,
                ["Database:ReadConnectionString"] = readConnectionString,
                ["Database:UseSeparateReadDb"] = "true"
            })
            .Build();

    private static void AssertDapperServicesResolve(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IConnectionFactory>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IDapperReadRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IDapperWriteRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
    }
}
