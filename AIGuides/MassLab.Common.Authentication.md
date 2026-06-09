# MassLab.Common.Authentication

Use this for JWT authentication, API key authentication, `ICurrentUser`, and JWT token generation.

NuGet:

```bash
dotnet add package MassLab.Common.Authentication
```

JWT setup:

```csharp
builder.Services.AddJwtAuthentication(builder.Configuration);

app.UseAuthentication();
app.UseAuthorization();
```

API key setup:

```csharp
builder.Services.AddApiKeyAuthentication(builder.Configuration);
```

Typical config sections: `Jwt`, `ApiKey`.

Read deeper only when needed: `MassLab.Common.Authentication/README.md`.

