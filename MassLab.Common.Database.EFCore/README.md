# MassLab.Common.Database.EFCore

Entity Framework Core implementation of the MassLab database abstractions.

## Overview

This library provides EF Core implementations of the repository pattern and Unit of Work pattern defined in `MassLab.Common.Database`. It supports both single database and separate read/write database configurations.

## Features

- **EFCoreReadRepository**: Read-only repository using EF Core with `AsNoTracking()` for optimal read performance
- **EFCoreWriteRepository**: Write repository using EF Core for Create, Update, Delete operations
- **EFCoreUnitOfWork**: Transaction management using `IDbContextTransaction`
- **Dependency Injection Extensions**: Easy registration of repositories and Unit of Work

## Installation

Add a project reference to your application:

```xml
<ProjectReference Include="..\MassLab.Common.Database.EFCore\MassLab.Common.Database.EFCore.csproj" />
```

## Usage

### Basic Configuration (Single Database)

```csharp
// In Program.cs or Startup.cs
services.AddEFCoreRepositories<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```

### Configuration with Separate Read/Write Databases

```csharp
services.AddEFCoreRepositoriesWithSeparateReadWrite<WriteDbContext, ReadDbContext>(
    writeOptions => writeOptions.UseSqlServer(configuration.GetConnectionString("WriteConnection")),
    readOptions => readOptions.UseSqlServer(configuration.GetConnectionString("ReadConnection")));
```

### Using Repositories in Handlers

```csharp
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IWriteRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateProductCommandHandler(
        IWriteRepository<Product> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(request.Name, request.Price, request.Description);
        
        await _repository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(product.Id);
    }
}
```

### Using Unit of Work for Transactions

```csharp
public async Task<Result> Handle(ComplexCommand request, CancellationToken cancellationToken)
{
    try
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        // Perform multiple operations
        await _productRepository.AddAsync(product, cancellationToken);
        await _categoryRepository.UpdateAsync(category, cancellationToken);
        
        await _unitOfWork.CommitTransactionAsync(cancellationToken);
        
        return Result.Success();
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        return Result.Failure(ex.Message);
    }
}
```

## Key Implementation Details

### Read Repository
- Uses `AsNoTracking()` for all queries to optimize read performance
- Does not track entity changes in the change tracker
- Ideal for read-only operations

### Write Repository
- Uses EF Core change tracking for Update and Delete operations
- `AddAsync` adds entities to the change tracker
- Changes are persisted when `SaveChangesAsync` is called on the Unit of Work

### Unit of Work
- Manages `DbContext.SaveChangesAsync()` for persisting changes
- Provides transaction management via `IDbContextTransaction`
- Ensures all operations within a transaction are committed or rolled back together
- Implements `IDisposable` for proper resource cleanup

## Requirements Validated

- **Requirement 1.5**: MassLab.Common.Database.EFCore library for EF Core implementations
- **Requirement 4.1**: EF Core repository supports both read and write operations
- **Requirement 5.3**: Repository supports connecting to either Write DB or Read DB
- **Requirement 10.1**: Repositories registered with DI container
- **Requirement 10.2**: Handlers registered with DI container
- **Requirement 10.3**: DbContext registered with scoped lifetime
- **Requirement 13.5**: EF Core repository uses Unit of Work to manage transactions

## Dependencies

- Microsoft.EntityFrameworkCore (>= 8.0.0)
- Microsoft.EntityFrameworkCore.Relational (>= 8.0.0)
- MassLab.Common.Database (project reference)
