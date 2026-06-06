# Victor.Common.Authentication

JWT bearer authentication, `ICurrentUser`, and `IJwtTokenService` for the
Victor framework.

## Wire-up

```csharp
// appsettings.json
"Jwt": {
  "Issuer":   "victor.local",
  "Audience": "victor.api",
  "SigningKey": "REPLACE_ME_WITH_A_LONG_SECRET_AT_LEAST_32_CHARS",
  "AccessTokenLifetime":  "01:00:00",
  "RefreshTokenLifetime": "30.00:00:00"
}

// Program.cs
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

## API key authentication

Use API keys for internal service-to-service endpoints or external/partner API
access. There is no single official HTTP API-key header, but `X-API-Key` is the
most common convention. The handler also accepts the legacy
`X-Internal-Api-Key` header by default.

```json
{
  "ApiKey": {
    "HeaderName": "X-API-Key",
    "ServiceHeaderName": "X-Service-Name",
    "ServiceName": "OrderApi",
    "ApiKey": "default-outbound-secret",
    "ApiKeys": {
      "ProductApi": "product-inbound-secret",
      "PartnerPortal": "partner-inbound-secret"
    },
    "Clients": {
      "product-api": {
        "ApiKey": "product-outbound-secret",
        "ServiceName": "OrderApi"
      },
      "stripe": {
        "HeaderName": "X-Stripe-Key",
        "ApiKey": "stripe-outbound-secret",
        "Headers": {
          "X-Api-Version": "2026-01-01"
        }
      }
    }
  }
}
```

```csharp
builder.Services.AddApiKeyAuthentication(builder.Configuration);

app.UseAuthentication();
app.UseAuthorization();

[Authorize(AuthenticationSchemes = ApiKeyDefaults.AuthenticationScheme)]
public class InternalController : ControllerBase
{
}
```

Expected inbound headers:

```http
X-API-Key: product-inbound-secret
X-Service-Name: ProductApi
```

Set `"RequireServiceName": true` when inbound keys must be paired with a known
service/client name. Leave it false for external APIs where the provider sends
only an API key.

For production, prefer storing SHA-256 hashes instead of raw keys:

```json
"ApiKey": {
  "StoreKeysAsSha256Hashes": true,
  "ApiKeys": {
    "ProductApi": "sha256-hex-value"
  }
}
```

## Issue tokens

```csharp
public class AuthController(IJwtTokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    public ActionResult Login(LoginRequest request)
    {
        // validate user
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, "Admin"),
        });
        return Ok(new {
            access  = tokens.GenerateToken(identity),
            refresh = tokens.GenerateRefreshToken(),
        });
    }
}
```

## Read the current user inside handlers

```csharp
public class CreateOrderCommandHandler(ICurrentUser user, IWriteRepository<Order> repo) :
    IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        if (!user.IsAuthenticated) return Result<Guid>.Failure("not authenticated");

        var order = new Order(user.UserId, cmd.Items);
        await repo.AddAsync(order, ct);
        return Result<Guid>.Success(order.Id);
    }
}
```
