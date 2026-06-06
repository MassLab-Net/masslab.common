# Victor.Common.Idempotency

Deduplicates HTTP write requests by `Idempotency-Key`. It uses
`ICacheService`, so register a cache provider first.

## Program.cs

```csharp
using Victor.Common.Caching.Redis.Extensions;
using Victor.Common.Idempotency.Extensions;

builder.Services.AddVictorRedisCache(builder.Configuration.GetSection("Redis"));
builder.Services.AddVictorIdempotency(builder.Configuration);

var app = builder.Build();
app.UseVictorIdempotency();
```

## Configuration

```json
{
  "Idempotency": {
    "HeaderName": "Idempotency-Key",
    "CacheKeyPrefix": "idempotency",
    "Expiration": "24:00:00",
    "Methods": [ "POST", "PUT", "PATCH" ],
    "RequireHeader": true
  }
}
```

## Client request

```http
POST /api/orders
Idempotency-Key: 5fba27c0-8b3e-438c-9d1d-58dbfb13f174
Content-Type: application/json
```

The first successful response is cached. Repeating the same method/path/key
returns the cached response instead of executing the handler again.

## Controller attribute

```csharp
[Idempotent]
[HttpPost("orders")]
public Task<IActionResult> Create(CreateOrderRequest request) { ... }
```
