# MassLab.Common.Database.EFCore.PostgreSQL

PostgreSQL database provider package for Entity Framework Core in the MassLab.Common.Database ecosystem.

## Overview

This package provides PostgreSQL-specific implementations for Entity Framework Core, enabling applications to use PostgreSQL databases with the MassLab.Common.Database repository pattern and unit of work abstractions.

## Dependencies

- `MassLab.Common.Database.EFCore` - Base EF Core implementations
- `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.0) - PostgreSQL provider for EF Core

## Installation

```bash
dotnet add package MassLab.Common.Database.EFCore.PostgreSQL
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

Register PostgreSQL services in your `Program.cs`:

```csharp
using MassLab.Common.Database.EFCore.PostgreSQL.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register PostgreSQL DbContext with repositories and unit of work
builder.Services.AddPostgreSqlDbContext<ApplicationDbContext>(
    builder.Configuration,
    configurationSection: "Database");

var app = builder.Build();
app.Run();
```

This registers:
- `ApplicationDbContext` configured with PostgreSQL
- `IReadRepository<T>` - For read operations
- `IWriteRepository<T>` - For write operations
- `IUnitOfWork` - For transaction management

### Separate Read/Write Configuration

For read replica scenarios, use separate DbContext instances:

```csharp
using MassLab.Common.Database.EFCore.PostgreSQL.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register separate read and write contexts
builder.Services.AddPostgreSqlDbContextWithSeparateReadWrite<WriteDbContext, ReadDbContext>(
    builder.Configuration,
    configurationSection: "Database");

var app = builder.Build();
app.Run();
```

Set `UseSeparateReadDb: true` in your configuration to enable read replicas:

```json
{
  "Database": {
    "WriteConnectionString": "Host=primary.db;Database=mydb;Username=user;Password=pass",
    "ReadConnectionString": "Host=replica.db;Database=mydb;Username=user;Password=pass",
    "UseSeparateReadDb": true
  }
}
```

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

## Custom Configuration Section

You can use a custom configuration section name:

```csharp
builder.Services.AddPostgreSqlDbContext<ApplicationDbContext>(
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

## Architecture

This package follows the provider separation pattern where:
- Base abstractions are defined in `MassLab.Common.Database`
- EF Core implementations are in `MassLab.Common.Database.EFCore`
- Provider-specific code (PostgreSQL) is isolated in this package

This allows applications to reference only the database providers they need, reducing dependency footprint and improving security.

## See Also

- [MassLab.Common.Database](../MassLab.Common.Database/) - Core abstractions
- [MassLab.Common.Database.EFCore](../MassLab.Common.Database.EFCore/) - EF Core base implementations
- [Npgsql Documentation](https://www.npgsql.org/doc/index.html) - PostgreSQL provider documentation
