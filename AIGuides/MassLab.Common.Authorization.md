# MassLab.Common.Authorization

Use this for permission and scope based authorization policies. It sits on top of authentication.

NuGet:

```bash
dotnet add package MassLab.Common.Authorization
```

Setup:

```csharp
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMassLabAuthorization();

app.UseAuthentication();
app.UseAuthorization();
```

Usage:

```csharp
[Authorize(Policy = "permission:products.write")]
[Authorize(Policy = "scope:orders.read")]
```

Read deeper only when needed: `MassLab.Common.Authorization/README.md`.

