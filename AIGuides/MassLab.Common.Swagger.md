# MassLab.Common.Swagger

Use this for Swagger/OpenAPI setup with JWT bearer support and per-version documents.

NuGet:

```bash
dotnet add package MassLab.Common.Swagger
```

Setup:

```csharp
builder.Services.AddMassLabApiVersioning();
builder.Services.AddSwaggerWithJwt(builder.Configuration);

app.UseSwaggerWithUI();
```

Config section: `Swagger`.

Read deeper only when needed: `MassLab.Common.Swagger/README.md`.

