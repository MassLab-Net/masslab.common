# MassLab.Common.Database.Dapper.MySQL

MySQL database provider package for Dapper in the MassLab.Common.Database ecosystem.

## Overview

This package provides MySQL-specific implementations for Dapper, enabling applications to use MySQL databases with the MassLab.Common.Database repository pattern and unit of work abstractions using Dapper's lightweight micro-ORM approach.

## Dependencies

- `MassLab.Common.Database.Dapper` - Base Dapper implementations
- `MySqlConnector` (v2.4.0) - MySQL ADO.NET provider

## Installation

```bash
dotnet add package MassLab.Common.Database.Dapper.MySQL
```

## Configuration

### Connection String Format

MySQL connection strings follow the MySqlConnector format:

```
Server=localhost;Port=3306;Database=mydb;User=myuser;Password=mypassword
```

### appsettings.json

Configure your database connection in `appsettings.json`:

```json
{
  "Database": {
    "WriteConnectionString": "Server=localhost;Database=mydb;User=root;Password=mypassword",
    "ReadConnectionString": "Server=replica.localhost;Database=mydb;User=root;Password=mypassword",
    "UseSeparateReadDb": false
  }
}
```

## Usage

### Basic Registration

Register MySQL Dapper services in your `Program.cs`:

```csharp
using MassLab.Common.Database.Dapper.MySQL.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register MySQL Dapper services with repositories and unit of work
builder.Services.AddMySqlDapper(
    builder.Configuration,
    configurationSection: "Database");

var app = builder.Build();
app.Run();
```

This registers:
- `MySqlConnectionFactory` as `IConnectionFactory`
- `IReadRepository<T>` - For read operations (uses read connection string)
- `IWriteRepository<T>` - For write operations (uses write connection string)
- `IUnitOfWork` - For transaction management

### Separate Read/Write Configuration

Dapper automatically supports separate read and write connections through the `DatabaseOptions` configuration:

```json
{
  "Database": {
    "WriteConnectionString": "Server=primary.db;Database=mydb;User=root;Password=mypassword",
    "ReadConnectionString": "Server=replica.db;Database=mydb;User=root;Password=mypassword",
    "UseSeparateReadDb": true
  }
}
```

When `UseSeparateReadDb` is `true`:
- `IReadRepository<T>` uses the `ReadConnectionString`
- `IWriteRepository<T>` and `IUnitOfWork` use the `WriteConnectionString`

When `UseSeparateReadDb` is `false`:
- All repositories use the `WriteConnectionString`

### Using Repositories

Inject and use repositories in your services:

```csharp
public class CustomerService
{
    private readonly IReadRepository<Customer> _readRepository;
    private readonly IWriteRepository<Customer> _writeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(
        IReadRepository<Customer> readRepository,
        IWriteRepository<Customer> writeRepository,
        IUnitOfWork unitOfWork)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _readRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _readRepository.GetAllAsync();
    }

    public async Task CreateAsync(Customer customer)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _writeRepository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
```

### Custom Dapper Queries

You can also inject `IConnectionFactory` directly for custom Dapper queries:

```csharp
public class CustomCustomerRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _connectionString;

    public CustomCustomerRepository(
        IConnectionFactory connectionFactory,
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _connectionString = configuration.GetSection("Database:ReadConnectionString").Value!;
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        using var connection = _connectionFactory.CreateConnection(_connectionString);
        return await connection.QueryAsync<Customer>(
            "SELECT * FROM Customers WHERE IsActive = @IsActive",
            new { IsActive = true });
    }
}
```

## Custom Configuration Section

You can use a custom configuration section name:

```csharp
builder.Services.AddMySqlDapper(
    builder.Configuration,
    configurationSection: "MySqlSettings");
```

```json
{
  "MySqlSettings": {
    "WriteConnectionString": "Server=localhost;Database=mydb;User=root;Password=mypassword"
  }
}
```

## Connection Management

The `MySqlConnectionFactory` provides connection caching through `GetOrCreateConnection()`, which reuses connections for the same connection string. This is useful for scenarios where you need to maintain the same connection instance across multiple operations.

For most scenarios, use `CreateConnection()` which creates a new connection instance each time.

## Architecture

This package follows the provider separation pattern where:
- Base abstractions are defined in `MassLab.Common.Database`
- Dapper implementations are in `MassLab.Common.Database.Dapper`
- Provider-specific code (MySQL) is isolated in this package

This allows applications to reference only the database providers they need, reducing dependency footprint and improving security.

## See Also

- [MassLab.Common.Database](../MassLab.Common.Database/) - Core abstractions
- [MassLab.Common.Database.Dapper](../MassLab.Common.Database.Dapper/) - Dapper base implementations
- [MySqlConnector Documentation](https://mysqlconnector.net/) - MySQL provider documentation
- [Dapper Documentation](https://github.com/DapperLib/Dapper) - Dapper micro-ORM documentation
