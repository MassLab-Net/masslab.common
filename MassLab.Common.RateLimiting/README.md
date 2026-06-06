# MassLab.Common.RateLimiting

Common ASP.NET Core rate limiting with fixed-window, sliding-window, and
token-bucket policies.

## Program.cs

```csharp
using MassLab.Common.RateLimiting.Extensions;

builder.Services.AddMassLabRateLimiting(builder.Configuration);

var app = builder.Build();
app.UseMassLabRateLimiting();
```

## Configuration

```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "UseUserPartitioning": true,
    "Policies": {
      "writes": {
        "Limiter": "TokenBucket",
        "PermitLimit": 20,
        "ReplenishmentSeconds": 10,
        "TokensPerPeriod": 5,
        "PartitionBy": "user"
      }
    }
  }
}
```

## Controller usage

```csharp
[EnableRateLimiting("writes")]
[HttpPost("products")]
public Task<IActionResult> Create(CreateProductRequest request) { ... }
```

When limits are exceeded the middleware returns `429` by default and emits
rate-limit response headers.
