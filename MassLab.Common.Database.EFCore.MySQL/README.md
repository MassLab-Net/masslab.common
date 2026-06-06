# MassLab.Common.Database.EFCore.MySQL

MySQL database provider package for Entity Framework Core in the MassLab.Common.Database ecosystem.

## Overview

This package provides MySQL-specific implementations for Entity Framework Core, enabling applications to use MySQL databases with the MassLab.Common.Database repository pattern and unit of work abstractions.

## Dependencies

- `MassLab.Common.Database.EFCore` - Base EF Core implementations
- `Pomelo.EntityFrameworkCore.MySql` (v9.0.0) - MySQL provider for EF Core

## Installation

```bash
dotnet add package MassLab.Common.Database.EFCore.MySQL
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

Register MySQL services in your `Program.cs`:

```csharp
using MassLab.Common.Database.EFCore.MySQL.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register MySQL DbContext with repositories and unit of work
builder.Services.AddMySqlDbContext<ApplicationDbContext>(
    builder.Configuration,
    configurationSection: "Database");

var app = builder.Build();
app.Run();
```

This registers:
- `ApplicationDbContext` configured with MySQL (with auto-detected server version)
- `IReadRepository<T>` - For read operations
- `IWriteRepository<T>` - For write operations
- `IUnitOfWork` - For transaction management

### Separate Read/Write Configuration

For read replica scenarios, use separate DbContext instances:

```csharp
using MassLab.Common.Database.EFCore.MySQL.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register separate read and write contexts
builder.Services.AddMySqlDbContextWithSeparateReadWrite<WriteDbContext, ReadDbContext>(
    builder.Configuration,
    configurationSection: "Database");

var app = builder.Build();
app.Run();
```

Set `UseSeparateReadDb: true` in your configuration to enable read replicas:

```json
{
  "Database": {
    "WriteConnectionString": "Server=primary.db;Database=mydb;User=root;Password=mypassword",
    "ReadConnectionString": "Server=replica.db;Database=mydb;User=root;Password=mypassword",
    "UseSeparateReadDb": true
  }
}
```

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

## Custom Configuration Section

You can use a custom configuration section name:

```csharp
builder.Services.AddMySqlDbContext<ApplicationDbContext>(
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

## Server Version Detection

This package uses `ServerVersion.AutoDetect()` to automatically determine the MySQL server version at startup. The server version is detected separately for read and write connections when using separate read/write configuration.

If you prefer to specify the server version explicitly, you would need to configure the DbContext manually rather than using the extension methods.

## Architecture

This package follows the provider separation pattern where:
- Base abstractions are defined in `MassLab.Common.Database`
- EF Core implementations are in `MassLab.Common.Database.EFCore`
- Provider-specific code (MySQL) is isolated in this package

This allows applications to reference only the database providers they need, reducing dependency footprint and improving security.

## See Also

- [MassLab.Common.Database](../MassLab.Common.Database/) - Core abstractions
- [MassLab.Common.Database.EFCore](../MassLab.Common.Database.EFCore/) - EF Core base implementations
- [Pomelo EF Core Provider Documentation](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql) - MySQL provider documentation
