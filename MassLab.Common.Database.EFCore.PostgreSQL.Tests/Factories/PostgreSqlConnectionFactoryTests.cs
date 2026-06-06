using System.Data;
using Npgsql;
using MassLab.Common.Database.EFCore.PostgreSQL.Factories;

namespace MassLab.Common.Database.EFCore.PostgreSQL.Tests.Factories;

/// <summary>
/// Unit tests for PostgreSqlConnectionFactory.
/// Validates Requirements 5.1, 5.7, 5.8
/// </summary>
public class PostgreSqlConnectionFactoryTests
{
    private const string ValidConnectionString = "Host=localhost;Database=test;Username=user;Password=pass";

    [Fact]
    public void CreateConnection_WithValidConnectionString_ReturnsNpgsqlConnection()
    {
        // Arrange
        var factory = new PostgreSqlConnectionFactory();

        // Act
        var connection = factory.CreateConnection(ValidConnectionString);

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<NpgsqlConnection>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateConnection_WithNullOrEmptyConnectionString_ThrowsArgumentException(string? connectionString)
    {
        // Arrange
        var factory = new PostgreSqlConnectionFactory();

        // Act
        var act = () => factory.CreateConnection(connectionString!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void GetOrCreateConnection_WithValidConnectionString_ReturnsNpgsqlConnection()
    {
        // Arrange
        var factory = new PostgreSqlConnectionFactory();

        // Act
        var connection = factory.GetOrCreateConnection(ValidConnectionString);

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<NpgsqlConnection>();
    }

    [Fact]
    public void GetOrCreateConnection_CalledTwiceWithSameConnectionString_ReturnsSameInstance()
    {
        // Arrange
        var factory = new PostgreSqlConnectionFactory();

        // Act
        var connection1 = factory.GetOrCreateConnection(ValidConnectionString);
        var connection2 = factory.GetOrCreateConnection(ValidConnectionString);

        // Assert
        connection1.Should().BeSameAs(connection2, "GetOrCreateConnection should cache connections");
    }

    [Fact]
    public void GetOrCreateConnection_CalledWithDifferentConnectionStrings_ReturnsDifferentInstances()
    {
        // Arrange
        var factory = new PostgreSqlConnectionFactory();
        var connectionString1 = "Host=localhost;Database=db1;Username=user;Password=pass";
        var connectionString2 = "Host=localhost;Database=db2;Username=user;Password=pass";

        // Act
        var connection1 = factory.GetOrCreateConnection(connectionString1);
        var connection2 = factory.GetOrCreateConnection(connectionString2);

        // Assert
        connection1.Should().NotBeSameAs(connection2, "Different connection strings should create different connections");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetOrCreateConnection_WithNullOrEmptyConnectionString_ThrowsArgumentException(string? connectionString)
    {
        // Arrange
        var factory = new PostgreSqlConnectionFactory();

        // Act
        var act = () => factory.GetOrCreateConnection(connectionString!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void CreateConnection_ReturnsNewInstanceEachTime()
    {
        // Arrange
        var factory = new PostgreSqlConnectionFactory();

        // Act
        var connection1 = factory.CreateConnection(ValidConnectionString);
        var connection2 = factory.CreateConnection(ValidConnectionString);

        // Assert
        connection1.Should().NotBeSameAs(connection2, "CreateConnection should always create new instances");
    }

    [Fact]
    public void GetOrCreateConnection_ImplementsIConnectionFactoryInterface()
    {
        // Arrange
        var factory = new PostgreSqlConnectionFactory();

        // Act
        IDbConnection connection = factory.GetOrCreateConnection(ValidConnectionString);

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeAssignableTo<IDbConnection>();
    }
}
