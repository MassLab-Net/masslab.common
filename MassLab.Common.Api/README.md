# MassLab.Common.Api

Common API utilities and middleware for ASP.NET Core applications in the MassLab framework.

## Features

- **Standardized API Responses**: `BaseApiResponse` and `BaseApiResponse<T>` for consistent response format
- **Pagination Support**: `IPagedRequest` and `PagedResponse<T>` for standardized pagination
- **Global Exception Handling**: Middleware for catching exceptions and returning RFC 7807 Problem Details
- **Trace ID Management**: Automatic trace ID generation and propagation for distributed tracing
- **CORS Configuration**: Easy CORS setup from configuration
- **Health Checks**: Built-in health check endpoints for monitoring application and dependencies
- **Custom Exceptions**: Domain-specific exceptions (NotFoundException, ConflictException, etc.)

## Installation

Add reference to your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\common\MassLab.Common.Api\MassLab.Common.Api.csproj" />
</ItemGroup>
```

## Usage

### 1. Standardized API Responses

All API responses use `BaseApiResponse` or `BaseApiResponse<T>`:

```csharp
// Success response with data
return BaseApiResponse<ProductDto>.Success(product);

// Error response
return BaseApiResponse.Failure("PRODUCT_NOT_FOUND", "Product not found");
```

Response format:
```json
{
  "isSuccess": true,
  "data": { "id": 1, "name": "Product" },
  "error": null,
  "traceId": "00-abc123...",
  "version": "1.0.0"
}
```

Error response format:
```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "PRODUCT_NOT_FOUND",
    "message": "Product not found"
  },
  "traceId": "00-abc123...",
  "version": "1.0.0"
}
```

### 2. Middleware Setup

Add middleware in `Program.cs`:

```csharp
var app = builder.Build();

// TraceId middleware (must be early in the pipeline)
app.UseTraceId();

// Global exception handling middleware
app.UseGlobalExceptionHandler();

// Other middleware...
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### 3. CORS Configuration

Add CORS in `Program.cs`:

```csharp
// Add CORS from configuration
builder.Services.AddCorsPolicy(builder.Configuration);

// Or with named policy
builder.Services.AddCorsPolicy(builder.Configuration, "MyPolicy");
```

Configure in `appsettings.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://yourdomain.com"
    ],
    "AllowCredentials": true,
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"],
    "AllowedHeaders": ["*"]
  }
}
```

Use in pipeline:

```csharp
// Use default policy
app.UseCors();

// Or use named policy
app.UseCors("MyPolicy");
```

### 4. Pagination

Implement pagination using `IPagedRequest` and `PagedResponse<T>`:

**Query with IPagedRequest:**
```csharp
public record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null) 
    : IRequest<BaseApiResponse<PagedResponse<ProductDto>>>, IPagedRequest
{
    // IPagedRequest provides Skip and Take properties automatically
}
```

**Handler:**
```csharp
public class GetProductsQueryHandler 
    : IRequestHandler<GetProductsQuery, BaseApiResponse<PagedResponse<ProductDto>>>
{
    private readonly ReadRepository<Product> _repository;

    public async Task<BaseApiResponse<PagedResponse<ProductDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _repository.AsQueryable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(p => p.Name.Contains(request.SearchTerm));
        }

        // Use extension method for pagination
        var (items, totalCount) = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto { ... })
            .ToPagedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        var pagedResponse = PagedResponse<ProductDto>.Create(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return BaseApiResponse<PagedResponse<ProductDto>>.Success(pagedResponse);
    }
}
```

**Controller:**
```csharp
[HttpGet]
public async Task<ActionResult<BaseApiResponse<PagedResponse<ProductDto>>>> GetProducts(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? searchTerm = null)
{
    var query = new GetProductsQuery(pageNumber, pageSize, searchTerm);
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

**Response format:**
```json
{
  "isSuccess": true,
  "data": {
    "items": [
      { "id": "...", "name": "Product 1", "price": 100 },
      { "id": "...", "name": "Product 2", "price": 200 }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 25,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "error": null,
  "traceId": "00-abc123...",
  "version": "1.0.0"
}
```

### 5. Health Checks

Add health checks in `Program.cs`:

```csharp
// Add health checks
builder.Services.AddCommonHealthChecks()
    .AddDatabaseHealthCheck<ApplicationDbContext>();

// Map health check endpoints
app.MapHealthCheckEndpoints();
```

This creates three endpoints:

- `GET /health` - Overall health (checks all dependencies)
- `GET /health/ready` - Readiness probe (checks if app is ready to receive traffic)
- `GET /health/live` - Liveness probe (checks if app is alive)

Response format:
```json
{
  "status": "Healthy",
  "totalDuration": 45.2,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database connection is healthy",
      "duration": 42.1,
      "exception": null,
      "data": {}
    }
  ]
}
```

Custom health checks can be added:

```csharp
builder.Services.AddCommonHealthChecks()
    .AddDatabaseHealthCheck<ApplicationDbContext>()
    .AddCheck("redis", () => 
    {
        // Your Redis health check logic
        return HealthCheckResult.Healthy("Redis is healthy");
    }, tags: new[] { "cache", "ready" });
```

### 6. Custom Exceptions

Throw domain-specific exceptions that are automatically handled:

```csharp
// Not found (404)
throw new NotFoundException("Product not found");

// Conflict (409)
throw new ConflictException("Product name already exists");

// Forbidden (403)
throw new ForbiddenException("Access denied");

// Unauthorized (401)
throw new UnauthorizedException("Invalid credentials");
```

These exceptions are caught by `GlobalExceptionMiddleware` and converted to appropriate HTTP responses.

### 7. Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<BaseApiResponse<int>>> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetProductById),
            new { id = result.Data },
            result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BaseApiResponse<ProductDto>>> GetProductById(
        int id,
        CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
```

## Components

### Models
- `BaseApiResponse`: Base response without data
- `BaseApiResponse<T>`: Generic response with typed data
- `ProblemDetailsResponse`: RFC 7807 Problem Details for errors

### Middleware
- `GlobalExceptionMiddleware`: Catches unhandled exceptions
- `TraceIdMiddleware`: Manages trace IDs for request correlation

### Exceptions
- `NotFoundException`: 404 Not Found
- `ConflictException`: 409 Conflict
- `ForbiddenException`: 403 Forbidden
- `UnauthorizedException`: 401 Unauthorized

### Extensions
- `ApplicationBuilderExtensions`: Middleware registration helpers
- `CorsServiceCollectionExtensions`: CORS configuration helpers
- `ResultExtensions`: (Legacy) Result to response conversion

## Configuration

### CorsOptions

```csharp
public class CorsOptions
{
    public string[] AllowedOrigins { get; set; }
    public bool AllowCredentials { get; set; }
    public string[] AllowedMethods { get; set; }
    public string[] AllowedHeaders { get; set; }
}
```

## Best Practices

1. **Always use BaseApiResponse**: Ensures consistent API responses
2. **Add TraceId middleware early**: Must be before other middleware to capture trace IDs
3. **Use custom exceptions**: Throw domain exceptions instead of returning error responses
4. **Configure CORS properly**: Only allow necessary origins in production
5. **Set assembly version**: Configure version in .csproj for accurate API versioning

## Dependencies

- Microsoft.AspNetCore.App (framework reference)
- No external package dependencies

## License

Internal use only - MassLab Framework
