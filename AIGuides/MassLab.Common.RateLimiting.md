# MassLab.Common.RateLimiting

Use this for ASP.NET Core fixed-window, sliding-window, and token-bucket rate limiting.

NuGet:

```bash
dotnet add package MassLab.Common.RateLimiting
```

Setup:

```csharp
builder.Services.AddMassLabRateLimiting(builder.Configuration);

app.UseMassLabRateLimiting();
```

Config section: `RateLimiting`.

Controller usage:

```csharp
[EnableRateLimiting("writes")]
```

Read deeper only when needed: `MassLab.Common.RateLimiting/README.md`.

