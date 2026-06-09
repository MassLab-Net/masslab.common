# MassLab.Common.Api

Use this when building ASP.NET Core APIs that need consistent responses, exception handling, trace id propagation, CORS helpers, health checks, security headers, or response compression.

NuGet:

```bash
dotnet add package MassLab.Common.Api
```

Main APIs:

```csharp
builder.Services.AddMassLabApi(builder.Configuration);
builder.Services.AddMassLabResponseCompression();

app.UseTraceId();
app.UseGlobalExceptionHandler();
app.UseRequestLogging();
app.UseSecurityHeaders();
```

Common types: `BaseApiResponse`, `BaseApiResponse<T>`, `PagedResponse<T>`, `Result`, `ProblemDetailsResponse`, `NotFoundException`, `ConflictException`, `ForbiddenException`, `UnauthorizedException`.

Read deeper only when needed: `MassLab.Common.Api/README.md`.

