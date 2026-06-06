# MassLab.Common.Database.Dapper.SqlServer

SQL Server database provider package for Dapper in the MassLab.Common.Database ecosystem.

## Overview

This package provides SQL Server-specific implementations for Dapper, enabling applications to use SQL Server databases with the MassLab.Common.Database repository pattern and unit of work abstractions using Dapper's lightweight micro-ORM approach.

## Dependencies

- `MassLab.Common.Database.Dapper` - Base Dapper implementations
- `Microsoft.Data.SqlClient` (v6.0.0) - SQL Server ADO.NET provider

## Installation

```bash
dotnet add package MassLab.Common.Database.Dapper.SqlServer
```

## Configuration

### Connection String Format

SQL Server connection strings follow the standard format:

```
Server=localhost;Database=mydb;User Id=myuser;Password=mypassword;TrustServerCertificate=True
```

Or with Windows Authentication:

```
Server=localhost;Database=mydb;Integrated Security=True;TrustServerCertificate=True
```

### appsettings.json

Configure your database connection in `appsettings.json`:

```json
{
  "Database": {
    "WriteConnectionString": "Server=localhost;Database=mydb;User Id=sa;Password=YourPassword123;TrustServerCertificate=True",
    "ReadConnectionString": "Server=replica.localhost;Database=mydb;User Id=sa;Password=YourPassword123;TrustServerCertificate=True",
    "UseSeparateReadDb": false
  }
}
```

## Usage

### Basic Registration

Register SQL Server Dapper services in your `Program.cs`:

```csharp
using MassLab.Common.Database.Dapper.SqlServer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register SQL Server Dapper services with repositories and unit of work
builder.Services.AddSqlServerDapper(
    builder.Configuration,
    configurationSection: "Database");

var app = builder.Build();
app.Run();
```

This registers:
- `SqlServerConnectionFactory` as `IConnectionFactory`
- `IReadRepository<T>` - For read operations (uses read connection string)
- `IWriteRepository<T>` - For write operations (uses write connection string)
- `IUnitOfWork` - For transaction management

### Separate Read/Write Configuration

Dapper automatically supports separate read and write connections through the `DatabaseOptions` configuration:

```json
{
  "Database": {
    "WriteConnectionString": "Server=primary.db;Database=mydb;User Id=sa;Password=Pass123;TrustServerCertificate=True",
    "ReadConnectionString": "Server=replica.db;Database=mydb;User Id=sa;Password=Pass123;TrustServerCertificate=True",
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
public class OrderService
{
    private readonly IReadRepository<Order> _readRepository;
    private readonly IWriteRepository<Order> _writeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IReadRepository<Order> readRepository,
        IWriteRepository<Order> writeRepository,
        IUnitOfWork unitOfWork)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _readRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _readRepository.GetAllAsync();
    }

    public async Task CreateAsync(Order order)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _writeRepository.AddAsync(order);
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
public class CustomOrderRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _connectionString;

    public CustomOrderRepository(
        IConnectionFactory connectionFactory,
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _connectionString = configuration.GetSection("Database:ReadConnectionString").Value!;
    }

    public async Task<IEnumerable<Order>> GetRecentOrdersAsync(int days)
    {
        using var connection = _connectionFactory.CreateConnection(_connectionString);
        return await connection.QueryAsync<Order>(
            "SELECT * FROM Orders WHERE OrderDate >= DATEADD(day, -@Days, GETDATE())",
            new { Days = days });
    }
}
```

## Custom Configuration Section

You can use a custom configuration section name:

```csharp
builder.Services.AddSqlServerDapper(
    builder.Configuration,
    configurationSection: "SqlServerSettings");
```

```json
{
  "SqlServerSettings": {
    "WriteConnectionString": "Server=localhost;Database=mydb;User Id=sa;Password=Pass123;TrustServerCertificate=True"
  }
}
```

## Connection Management

The `SqlServerConnectionFactory` provides connection caching through `GetOrCreateConnection()`, which reuses connections for the same connection string. This is useful for scenarios where you need to maintain the same connection instance across multiple operations.

For most scenarios, use `CreateConnection()` which creates a new connection instance each time.

## Architecture

This package follows the provider separation pattern where:
- Base abstractions are defined in `MassLab.Common.Database`
- Dapper implementations are in `MassLab.Common.Database.Dapper`
- Provider-specific code (SQL Server) is isolated in this package

This allows applications to reference only the database providers they need, reducing dependency footprint and improving security.

## See Also

- [MassLab.Common.Database](../MassLab.Common.Database/) - Core abstractions
- [MassLab.Common.Database.Dapper](../MassLab.Common.Database.Dapper/) - Dapper base implementations
- [Microsoft.Data.SqlClient Documentation](https://learn.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace) - SQL Server provider documentation
- [Dapper Documentation](https://github.com/DapperLib/Dapper) - Dapper micro-ORM documentation
