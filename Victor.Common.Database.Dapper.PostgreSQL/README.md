# Victor.Common.Database.Dapper.PostgreSQL

PostgreSQL database provider package for Dapper in the Victor.Common.Database ecosystem.

## Overview

This package provides PostgreSQL-specific implementations for Dapper, enabling applications to use PostgreSQL databases with the Victor.Common.Database repository pattern and unit of work abstractions using Dapper's lightweight micro-ORM approach.

## Dependencies

- `Victor.Common.Database.Dapper` - Base Dapper implementations
- `Npgsql` (v10.0.0) - PostgreSQL ADO.NET provider

## Installation

```bash
dotnet add package Victor.Common.Database.Dapper.PostgreSQL
```

## Configuration

### Connection String Format

PostgreSQL connection strings follow the Npgsql format:

```
Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword
```

### appsettings.json

Configure your database connection in `appsettings.json`:

```json
{
  "Database": {
    "WriteConnectionString": "Host=localhost;Database=mydb;Username=user;Password=pass",
    "ReadConnectionString": "Host=replica.localhost;Database=mydb;Username=user;Password=pass",
    "UseSeparateReadDb": false
  }
}
```

## Usage

### Basic Registration

Register PostgreSQL Dapper services in your `Program.cs`:

```csharp
using Victor.Common.Database.Dapper.PostgreSQL.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register PostgreSQL Dapper services with repositories and unit of work
builder.Services.AddPostgreSqlDapper(
    builder.Configuration,
    configurationSection: "Database");

var app = builder.Build();
app.Run();
```

This registers:
- `PostgreSqlConnectionFactory` as `IConnectionFactory`
- `IReadRepository<T>` - For read operations (uses read connection string)
- `IWriteRepository<T>` - For write operations (uses write connection string)
- `IUnitOfWork` - For transaction management

### Separate Read/Write Configuration

Dapper automatically supports separate read and write connections through the `DatabaseOptions` configuration:

```json
{
  "Database": {
    "WriteConnectionString": "Host=primary.db;Database=mydb;Username=user;Password=pass",
    "ReadConnectionString": "Host=replica.db;Database=mydb;Username=user;Password=pass",
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
public class ProductService
{
    private readonly IReadRepository<Product> _readRepository;
    private readonly IWriteRepository<Product> _writeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IReadRepository<Product> readRepository,
        IWriteRepository<Product> writeRepository,
        IUnitOfWork unitOfWork)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _readRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _readRepository.GetAllAsync();
    }

    public async Task CreateAsync(Product product)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _writeRepository.AddAsync(product);
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
public class CustomProductRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _connectionString;

    public CustomProductRepository(
        IConnectionFactory connectionFactory,
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _connectionString = configuration.GetSection("Database:ReadConnectionString").Value!;
    }

    public async Task<IEnumerable<Product>> GetExpensiveProductsAsync(decimal minPrice)
    {
        using var connection = _connectionFactory.CreateConnection(_connectionString);
        return await connection.QueryAsync<Product>(
            "SELECT * FROM Products WHERE Price >= @MinPrice",
            new { MinPrice = minPrice });
    }
}
```

## Custom Configuration Section

You can use a custom configuration section name:

```csharp
builder.Services.AddPostgreSqlDapper(
    builder.Configuration,
    configurationSection: "PostgreSqlSettings");
```

```json
{
  "PostgreSqlSettings": {
    "WriteConnectionString": "Host=localhost;Database=mydb;Username=user;Password=pass"
  }
}
```

## Connection Management

The `PostgreSqlConnectionFactory` provides connection caching through `GetOrCreateConnection()`, which reuses connections for the same connection string. This is useful for scenarios where you need to maintain the same connection instance across multiple operations.

For most scenarios, use `CreateConnection()` which creates a new connection instance each time.

## Architecture

This package follows the provider separation pattern where:
- Base abstractions are defined in `Victor.Common.Database`
- Dapper implementations are in `Victor.Common.Database.Dapper`
- Provider-specific code (PostgreSQL) is isolated in this package

This allows applications to reference only the database providers they need, reducing dependency footprint and improving security.

## See Also

- [Victor.Common.Database](../Victor.Common.Database/) - Core abstractions
- [Victor.Common.Database.Dapper](../Victor.Common.Database.Dapper/) - Dapper base implementations
- [Npgsql Documentation](https://www.npgsql.org/doc/index.html) - PostgreSQL provider documentation
- [Dapper Documentation](https://github.com/DapperLib/Dapper) - Dapper micro-ORM documentation
