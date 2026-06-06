# MassLab.Common.Swagger

Swagger/OpenAPI registration for MassLab services, including API version
documents and an optional JWT bearer authorize button.

## Program.cs

```csharp
using MassLab.Common.ApiVersioning.Extensions;
using MassLab.Common.Swagger.Extensions;

builder.Services.AddMassLabApiVersioning();
builder.Services.AddSwaggerWithJwt(builder.Configuration);

var app = builder.Build();
app.UseSwaggerWithUI();
```

## Configuration

```json
{
  "Swagger": {
    "Title": "Product API",
    "Description": "Product service endpoints",
    "EnableJwtBearer": true,
    "RoutePrefix": "swagger"
  }
}
```

Open `/swagger` to inspect API docs. When API versioning is enabled, Swagger UI
shows one document per discovered API version.
