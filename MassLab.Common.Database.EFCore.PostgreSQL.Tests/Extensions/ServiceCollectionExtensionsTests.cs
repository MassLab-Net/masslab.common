using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.Database.Abstractions;
using MassLab.Common.Database.Configuration;
using MassLab.Common.Database.EFCore.PostgreSQL.Extensions;
using Xunit;

namespace MassLab.Common.Database.EFCore.PostgreSQL.Tests.Extensions;

/// <summary>
/// Unit tests for ServiceCollectionExtensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Test DbContext for testing purposes.
    /// </summary>
    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }

    /// <summary>
    /// Test write DbContext for separate read/write testing.
    /// </summary>
    private class TestWriteDbContext : DbContext
    {
        public TestWriteDbContext(DbContextOptions<TestWriteDbContext> options) : base(options)
        {
        }
    }

    /// <summary>
    /// Test read DbContext for separate read/write testing.
    /// </summary>
    private class TestReadDbContext : DbContext
    {
        public TestReadDbContext(DbContextOptions<TestReadDbContext> options) : base(options)
        {
        }
    }

    /// <summary>
    /// Test entity for repository testing.
    /// </summary>
    private class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void AddPostgreSqlDbContext_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:WriteConnectionString"] = "Host=localhost;Database=test;Username=user;Password=pass",
                ["Database:ReadConnectionString"] = "",
                ["Database:UseSeparateReadDb"] = "false"
            })
            .Build();

        // Act
        services.AddPostgreSqlDbContext<TestDbContext>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify all required services are registered
        Assert.NotNull(serviceProvider.GetService<TestDbContext>());
        Assert.NotNull(serviceProvider.GetService<DbContext>());
        Assert.NotNull(serviceProvider.GetService<IReadRepository<object>>());
        Assert.NotNull(serviceProvider.GetService<IWriteRepository<object>>());
        Assert.NotNull(serviceProvider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void AddPostgreSqlDbContext_BindsDatabaseOptionsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedConnectionString = "Host=10.41.135.69;Database=ecp_test_dev;Username=user;Password=pass";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:WriteConnectionString"] = expectedConnectionString,
                ["Database:ReadConnectionString"] = "",
                ["Database:UseSeparateReadDb"] = "false"
            })
            .Build();

        // Act
        services.AddPostgreSqlDbContext<TestDbContext>(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(expectedConnectionString, options.Value.WriteConnectionString);
    }

    [Fact]
    public void AddPostgreSqlDbContext_WithCustomConfigurationSection_BindsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedConnectionString = "Host=localhost;Database=custom;Username=user;Password=pass";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomSection:WriteConnectionString"] = expectedConnectionString,
                ["CustomSection:ReadConnectionString"] = "",
                ["CustomSection:UseSeparateReadDb"] = "false"
            })
            .Build();

        // Act
        services.AddPostgreSqlDbContext<TestDbContext>(configuration, "CustomSection");
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(expectedConnectionString, options.Value.WriteConnectionString);
    }

    [Fact]
    public void AddPostgreSqlDbContextWithSeparateReadWrite_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:WriteConnectionString"] = "Host=localhost;Database=write;Username=user;Password=pass",
                ["Database:ReadConnectionString"] = "Host=localhost;Database=read;Username=user;Password=pass",
                ["Database:UseSeparateReadDb"] = "true"
            })
            .Build();

        // Act
        services.AddPostgreSqlDbContextWithSeparateReadWrite<TestWriteDbContext, TestReadDbContext>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify all required services are registered
        Assert.NotNull(serviceProvider.GetService<TestWriteDbContext>());
        Assert.NotNull(serviceProvider.GetService<TestReadDbContext>());
        Assert.NotNull(serviceProvider.GetService<IReadRepository<TestEntity>>());
        Assert.NotNull(serviceProvider.GetService<IWriteRepository<TestEntity>>());
        Assert.NotNull(serviceProvider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void AddPostgreSqlDbContextWithSeparateReadWrite_BindsDatabaseOptionsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedWriteConnectionString = "Host=10.41.135.69;Database=write_db;Username=user;Password=pass";
        var expectedReadConnectionString = "Host=10.41.135.70;Database=read_db;Username=user;Password=pass";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:WriteConnectionString"] = expectedWriteConnectionString,
                ["Database:ReadConnectionString"] = expectedReadConnectionString,
                ["Database:UseSeparateReadDb"] = "true"
            })
            .Build();

        // Act
        services.AddPostgreSqlDbContextWithSeparateReadWrite<TestWriteDbContext, TestReadDbContext>(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(expectedWriteConnectionString, options.Value.WriteConnectionString);
        Assert.Equal(expectedReadConnectionString, options.Value.ReadConnectionString);
    }

    [Fact]
    public void AddPostgreSqlDbContextWithSeparateReadWrite_WithCustomConfigurationSection_BindsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedWriteConnectionString = "Host=localhost;Database=custom_write;Username=user;Password=pass";
        var expectedReadConnectionString = "Host=localhost;Database=custom_read;Username=user;Password=pass";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomSection:WriteConnectionString"] = expectedWriteConnectionString,
                ["CustomSection:ReadConnectionString"] = expectedReadConnectionString,
                ["CustomSection:UseSeparateReadDb"] = "true"
            })
            .Build();

        // Act
        services.AddPostgreSqlDbContextWithSeparateReadWrite<TestWriteDbContext, TestReadDbContext>(configuration, "CustomSection");
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(expectedWriteConnectionString, options.Value.WriteConnectionString);
        Assert.Equal(expectedReadConnectionString, options.Value.ReadConnectionString);
    }
}
