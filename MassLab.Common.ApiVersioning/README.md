# MassLab.Common.ApiVersioning

Registers ASP.NET API versioning with API explorer support. Use it when services
need versioned controllers and one Swagger document per API version.

## Program.cs

```csharp
using MassLab.Common.ApiVersioning.Extensions;

builder.Services.AddControllers();
builder.Services.AddMassLabApiVersioning();
```

## Controller usage

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id) => Ok();
}
```

The registration enables URL segment versioning and `api-supported-versions`
response headers, and exposes version metadata to Swagger.
