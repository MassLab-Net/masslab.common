# MassLab.Common.RateLimiting

ASP.NET Core rate limiting with dynamic per-user/IP policies, endpoint overrides, and wildcard support.

## Quick Start

```csharp
// Program.cs
using MassLab.Common.RateLimiting.Extensions;

builder.Services.AddMassLabRateLimiting(builder.Configuration);

var app = builder.Build();
app.UseMassLabRateLimiting();
```

## Configuration
Simple (IP-based, shared bucket)
```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "PartitionBy": "ip"
  }
}
```
### User-based with per-user policies
```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "PartitionBy": "user",
    
    "UserPartition": {
      "ClaimName": "sub",
      "Policies": {
        "premium-user-123": {
          "DefaultLimit": { "PermitLimit": 500 },
          "EndpointOverrides": {
            "/api/export/*": { "PermitLimit": 5, "WindowSeconds": 3600 }
          }
        }
      }
    }
  }
}
```

### Per-endpoint rate limiting
``` json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "PerEndpoint": true
  }
}
```
PerEndpoint: false → All APIs share 100 req/min
PerEndpoint: true → Each API gets 100 req/min

### Mix User and IP
``` json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "Limiter": "FixedWindow",
    "QueueLimit": 0,
    "RejectionStatusCode": 429,
    "PerEndpoint": false,
    "PartitionBy": "user",
    
    "SegmentsPerWindow": 4,
    "ReplenishmentSeconds": 10,
    "TokensPerPeriod": 10,
    
    "UserPartition": {
      "ClaimName": "sub",
      "Policies": {
        "premium-user-123": {
          "DefaultLimit": {
            "Limiter": "TokenBucket",
            "PermitLimit": 500,
            "TokensPerPeriod": 50,
            "ReplenishmentSeconds": 10,
            "PerEndpoint": false
          },
          "EndpointOverrides": {
            "/api/export/*": {
              "PermitLimit": 5,
              "WindowSeconds": 3600
            },
            "/api/ai/*": {
              "Limiter": "SlidingWindow",
              "PermitLimit": 100,
              "WindowSeconds": 60,
              "SegmentsPerWindow": 6
            }
          }
        }
      }
    },
    
    "IpPartition": {
      "Policies": {
        "10.0.0.*": {
          "DefaultLimit": {
            "PermitLimit": 500
          }
        }
      }
    },
    
    "Policies": {
      "writes": {
        "Limiter": "TokenBucket",
        "PermitLimit": 20,
        "TokensPerPeriod": 5,
        "ReplenishmentSeconds": 10,
        "PartitionBy": "user"
      },
      "strict": {
        "PermitLimit": 10,
        "WindowSeconds": 60,
        "PartitionBy": "ip",
        "PerEndpoint": true
      }
    }
  }
}

```

Controller Usage
``` csharp
// Auto rate limit (uses Global Limiter)
[HttpGet("products")]
public Task<IActionResult> GetAll() { ... }

// Named policy
[EnableRateLimiting("writes")]
[HttpPost("products")]
public Task<IActionResult> Create() { ... }

// Disable rate limiting
[DisableRateLimiting]
[HttpGet("health")]
public Task<IActionResult> Health() { ... }
```


## Supported Limiters
- `FixedWindow` (default)
- `SlidingWindow`
- `TokenBucket`
