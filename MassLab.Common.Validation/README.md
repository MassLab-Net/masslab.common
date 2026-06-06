# MassLab.Common.Validation

A common validation library that integrates FluentValidation with MediatR pipeline for automatic validation of commands and queries.

## Overview

This library provides a reusable validation infrastructure that automatically validates MediatR requests (commands and queries) before they reach their handlers. If validation fails, a `ValidationException` is thrown with detailed error information, preventing the handler from executing.

## Features

- **Automatic Validation**: Validates all MediatR requests through a pipeline behavior
- **FluentValidation Integration**: Uses FluentValidation for defining validation rules
- **Structured Error Responses**: Returns validation errors grouped by property name
- **Easy Registration**: Simple extension methods for dependency injection setup
- **Reusable Across Microservices**: Designed to be shared across all services in the MassLab architecture

## Installation

Add a project reference to `MassLab.Common.Validation` in your application project:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\common\MassLab.Common.Validation\MassLab.Common.Validation.csproj" />
</ItemGroup>
```

## Usage

### 1. Define Validators

Create validators for your commands and queries using FluentValidation:

```csharp
using FluentValidation;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
        
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
    }
}
```

### 2. Register Validation Services

In your `Program.cs`, register the validation services:

```csharp
using MassLab.Common.Validation.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Register validation from the assembly containing your validators
builder.Services.AddValidation(typeof(CreateProductCommandValidator).Assembly);
```

### 3. Handle ValidationException

The `ValidationException` is automatically thrown when validation fails. You can handle it in your global exception middleware:

```csharp
catch (ValidationException validationEx)
{
    var problemDetails = new ProblemDetailsResponse
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        Title = "Validation Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = "One or more validation errors occurred.",
        Instance = context.Request.Path,
        TraceId = traceId,
        Errors = validationEx.Errors
    };
    
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    await context.Response.WriteAsJsonAsync(problemDetails);
}
```

## Components

### ValidationException

Exception thrown when validation fails. Contains a dictionary of validation errors grouped by property name.

```csharp
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }
}
```

### ValidationBehavior<TRequest, TResponse>

MediatR pipeline behavior that executes all registered validators for a request before the handler executes.

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Executes validators and throws ValidationException on failure
}
```

### ServiceCollectionExtensions

Extension methods for registering validation services:

```csharp
public static IServiceCollection AddValidation(
    this IServiceCollection services,
    Assembly assembly)
{
    // Registers validators and ValidationBehavior
}
```

## Validation Flow

```
API Request
    ↓
Controller
    ↓
MediatR.Send(command)
    ↓
ValidationBehavior
    ↓
Execute All Validators
    ↓
Valid? ──No──→ Throw ValidationException ──→ GlobalExceptionMiddleware ──→ HTTP 400
    ↓
   Yes
    ↓
Command/Query Handler
    ↓
Success Response
```

## Error Response Format

When validation fails, the exception contains errors in this format:

```json
{
  "Name": ["Name is required", "Name must not exceed 200 characters"],
  "Price": ["Price must be greater than 0"]
}
```

This integrates seamlessly with RFC 7807 Problem Details responses when used with `MassLab.Common.Api`.

## Requirements Satisfied

- **Requirement 14.1**: Validation library with ValidationException containing Errors dictionary
- **Requirement 14.3**: Automatic validator registration from assemblies
- **Requirement 14.4**: Automatic validation execution before handlers via MediatR pipeline
- **Requirement 14.5**: Returns validation errors without executing handler on failure
- **Requirement 14.6**: MediatR pipeline behavior for automatic validation
- **Requirement 14.7**: Reusable validation infrastructure across microservices

## Dependencies

- FluentValidation (11.9.0)
- FluentValidation.DependencyInjectionExtensions (11.9.0)
- MediatR (12.2.0)

## License

This library is part of the MassLab project structure and follows the same licensing terms as the parent project.
