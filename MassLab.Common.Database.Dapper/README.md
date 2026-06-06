# MassLab.Common.Database.Dapper

Dapper-based repositories for high-performance raw SQL operations with separate read/write database support.

## Philosophy

Dapper is a micro-ORM focused on raw SQL performance. This package embraces that philosophy by providing simple repositories that execute raw SQL queries and commands, rather than trying to mimic full ORM behavior.

## Repositories

### DapperReadRepository
- Uses read database connection
- For SELECT queries
- Optimized for read-heavy workloads
- Supports pagination

### DapperWriteRepository
- Uses write database connection
- For INSERT, UPDATE, DELETE commands
- Ensures write operations go to primary database

## Registration

```csharp
// In ServiceCollectionExtensions.cs
services.AddScoped<IDapperReadRepository>(sp =>
{
    var connectionFactory = sp.GetRequiredService<IConnectionFactory>();
    var options = sp.GetRequiredService<IOptions<DatabaseOptions>>();
    return new DapperReadRepository(connectionFactory, options.Value.GetReadConnectionString());
});

services.AddScoped<IDapperWriteRepository>(sp =>
{
    var connectionFactory = sp.GetRequiredService<IConnectionFactory>();
    var options = sp.GetRequiredService<IOptions<DatabaseOptions>>();
    return new DapperWriteRepository(connectionFactory, options.Value.GetWriteConnectionString());
});
```

## Usage Examples

### Read Operations

```csharp
public class ProductService
{
    private readonly IDapperReadRepository _dapperRead;

    public async Task<IEnumerable<Product>> GetExpensiveProducts()
    {
        return await _dapperRead.QueryAsync<Product>(
            "SELECT * FROM Products WHERE Price > @MinPrice ORDER BY Price DESC",
            new { MinPrice = 1000 });
    }

    public async Task<Product?> GetProductById(Guid id)
    {
        return await _dapperRead.QueryFirstOrDefaultAsync<Product>(
            "SELECT * FROM Products WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<PagedResponse<Product>> GetProductsPaged(int page, int pageSize)
    {
        var sql = "SELECT * FROM Products ORDER BY CreatedAt DESC";
        var (items, totalCount) = await _dapperRead.QueryPagedAsync<Product>(sql, page, pageSize);
        
        return new PagedResponse<Product>(items, totalCount, page, pageSize);
    }

    // QueryMultipleAsync - Execute multiple queries in one round-trip
    public async Task<ProductWithStats> GetProductWithStats(Guid id)
    {
        var sql = @"
            SELECT * FROM Products WHERE Id = @Id;
            SELECT COUNT(*) as OrderCount, SUM(Quantity) as TotalSold 
            FROM Orders WHERE ProductId = @Id;
            SELECT * FROM Reviews WHERE ProductId = @Id ORDER BY CreatedAt DESC LIMIT 5";

        return await _dapperRead.QueryMultipleAsync(sql, async (grid) =>
        {
            var product = await grid.ReadFirstOrDefaultAsync<Product>();
            var stats = await grid.ReadFirstOrDefaultAsync<OrderStats>();
            var reviews = await grid.ReadAsync<Review>();
            
            return new ProductWithStats
            {
                Product = product,
                Stats = stats,
                RecentReviews = reviews.ToList()
            };
        }, new { Id = id });
    }
}
```

### Write Operations

```csharp
public class ProductService
{
    private readonly IDapperWriteRepository _dapperWrite;

    public async Task<int> BulkUpdatePrices(decimal multiplier)
    {
        return await _dapperWrite.ExecuteAsync(
            "UPDATE Products SET Price = Price * @Multiplier WHERE IsActive = true",
            new { Multiplier = multiplier });
    }

    public async Task<int> GetProductCount()
    {
        return await _dapperWrite.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Products WHERE IsActive = true") ?? 0;
    }
}
```

## Connection Management

### Connection Pooling (Default Behavior)

By default, Dapper repositories create new connections for each operation using `IConnectionFactory`. However, these connections are **NOT new physical connections** - they are managed by ADO.NET's connection pool:

```csharp
using var connection = _connectionFactory.CreateConnection(_connectionString);
await connection.OpenAsync(cancellationToken);
// Connection is retrieved from the pool (very fast, ~microseconds)
// When disposed, it returns to the pool, NOT closed
```

**Benefits:**
- ✅ Very fast (connection pooling handles physical connections)
- ✅ Automatic connection lifecycle management
- ✅ No connection leaks (using statement ensures disposal)
- ✅ Thread-safe

**Performance:**
- Getting a connection from pool: **~1-10 microseconds**
- Creating a new physical connection: **~10-100 milliseconds**
- Connection pooling makes "new connection" operations negligible

### Sharing Connection with EF Core

If you need Dapper and EF Core to share the **same transaction**, you have two options:

### Sharing Connection with EF Core

When you need Dapper and EF Core to share the same transaction:

```csharp
public class OrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IDapperWriteRepository _dapperWrite;

    public async Task CreateOrderWithInventoryUpdate(CreateOrderCommand cmd)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // EF Core: Create order
            var order = new Order(cmd.ProductId, cmd.Quantity);
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Share connection with Dapper
            var connection = _context.Database.GetDbConnection();
            var dbTransaction = transaction.GetDbTransaction();
            _dapperWrite.SetConnection(connection, dbTransaction);

            // Dapper: Bulk update inventory (same transaction)
            await _dapperWrite.ExecuteAsync(
                "UPDATE Inventory SET Quantity = Quantity - @Qty WHERE ProductId = @ProductId",
                new { Qty = cmd.Quantity, ProductId = cmd.ProductId });

            await transaction.CommitAsync();
            _dapperWrite.ClearConnection();
        }
        catch
        {
            await transaction.RollbackAsync();
            _dapperWrite.ClearConnection();
            throw;
        }
    }
}
```

### Direct Connection Usage (Without Repository)

You can also use Dapper directly on EF Core's connection:

```csharp
public async Task<IEnumerable<Product>> GetProducts()
{
    var connection = _context.Database.GetDbConnection();
    
    if (connection.State != ConnectionState.Open)
        await connection.OpenAsync();

    return await connection.QueryAsync<Product>(
        "SELECT * FROM Products WHERE Price > @MinPrice",
        new { MinPrice = 100 });
}
```

## When to Use What

### Use DapperReadRepository
- Complex queries with joins, CTEs, window functions
- Performance-critical read operations
- Reporting and analytics queries
- Read from read replicas

### Use DapperWriteRepository
- Bulk operations (updates, deletes)
- Custom SQL that doesn't fit ORM patterns
- Write to primary database

### Use EF Core
- CRUD operations on single entities
- Navigation property loading
- Change tracking
- Migrations and schema management

### Share Connection/Transaction
- Multiple operations must be atomic
- Complex business logic spanning both EF Core and Dapper
- Need guaranteed data consistency

## Complete Example

```csharp
public class ProductService
{
    private readonly ApplicationDbContext _context;
    private readonly IDapperReadRepository _dapperRead;
    private readonly IDapperWriteRepository _dapperWrite;

    // Scenario 1: Separate operations (no transaction needed)
    public async Task<ProductDto> GetProductWithStats(Guid id)
    {
        // Dapper for complex query (read replica)
        var stats = await _dapperRead.QueryFirstOrDefaultAsync<ProductStats>(
            @"SELECT ProductId, 
                     COUNT(*) as OrderCount,
                     SUM(Quantity) as TotalSold,
                     AVG(Price) as AvgPrice
              FROM Orders 
              WHERE ProductId = @Id
              GROUP BY ProductId",
            new { Id = id });

        // EF Core for entity (primary database)
        var product = await _context.Products.FindAsync(id);

        return new ProductDto { Product = product, Stats = stats };
    }

    // Scenario 2: Transactional operations (shared transaction)
    public async Task CreateProductWithInventory(CreateProductCommand cmd)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // EF Core: Create product
            var product = new Product(cmd.Name, cmd.Price, cmd.Description);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Dapper: Bulk insert inventory (same transaction)
            var connection = _context.Database.GetDbConnection();
            _dapperWrite.SetConnection(connection, transaction.GetDbTransaction());
            
            await _dapperWrite.ExecuteAsync(
                @"INSERT INTO Inventory (ProductId, Warehouse, Quantity)
                  SELECT @ProductId, WarehouseId, 0 FROM Warehouses",
                new { ProductId = product.Id });

            await transaction.CommitAsync();
            _dapperWrite.ClearConnection();
        }
        catch
        {
            await transaction.RollbackAsync();
            _dapperWrite.ClearConnection();
            throw;
        }
    }

    // Scenario 3: Pure Dapper for performance
    public async Task<IEnumerable<ProductReport>> GetProductReport(DateTime startDate, DateTime endDate)
    {
        return await _dapperRead.QueryAsync<ProductReport>(
            @"WITH ProductSales AS (
                  SELECT p.Id, p.Name, p.Price,
                         COUNT(o.Id) as OrderCount,
                         SUM(o.Quantity) as TotalSold,
                         SUM(o.Quantity * o.Price) as Revenue
                  FROM Products p
                  LEFT JOIN Orders o ON p.Id = o.ProductId
                  WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate
                  GROUP BY p.Id, p.Name, p.Price
              )
              SELECT * FROM ProductSales
              ORDER BY Revenue DESC",
            new { StartDate = startDate, EndDate = endDate });
    }
}
```

## Summary

- **Two repositories: Read and Write** - Separate database connections
- **Non-generic, SQL-focused** - Embrace Dapper's raw SQL strength
- **Connection pooling is automatic** - Fast and efficient
- **Share transactions when needed** - For atomic operations
- **Use each tool for its strength** - EF Core for ORM, Dapper for raw SQL
