# Victor.Common.Authorization

Permission and scope authorization helpers for Victor services. This package is
intended to sit on top of JWT or API-key authentication.

## Program.cs

```csharp
using Victor.Common.Authentication.Extensions;
using Victor.Common.Authorization.Extensions;

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddVictorAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

## Controller usage

```csharp
[Authorize(Policy = "permission:products.write")]
[HttpPost("products")]
public Task<IActionResult> Create(CreateProductRequest request)
{
    // Current user must have a permission claim containing products.write.
}

[Authorize(Policy = "scope:orders.read")]
[HttpGet("orders")]
public Task<IActionResult> GetOrders()
{
    // Current user must have a scope/scp claim containing orders.read.
}
```

Policies are created lazily and cached. Supported claim styles include
space-delimited `scope`/`scp` values and permission claims.
