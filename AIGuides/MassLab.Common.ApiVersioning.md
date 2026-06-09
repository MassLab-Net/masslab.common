# MassLab.Common.ApiVersioning

Use this when APIs need versioned routes and API explorer metadata for Swagger.

NuGet:

```bash
dotnet add package MassLab.Common.ApiVersioning
```

Main API:

```csharp
builder.Services.AddControllers();
builder.Services.AddMassLabApiVersioning();
```

Controller pattern:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ControllerBase;
```

Read deeper only when needed: `MassLab.Common.ApiVersioning/README.md`.

