# MassLab.Common.Validation

Use this for FluentValidation validators integrated into the MediatR pipeline.

NuGet:

```bash
dotnet add package MassLab.Common.Validation
```

Setup:

```csharp
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddValidation(typeof(CreateProductCommandValidator).Assembly);
```

Validators throw `ValidationException` before handlers execute. Pair with `MassLab.Common.Api` global exception handling for API responses.

Read deeper only when needed: `MassLab.Common.Validation/README.md`.

