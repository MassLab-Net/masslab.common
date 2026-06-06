# MassLab.Common.Logging

Common logging abstractions and configuration for MassLab microservices.

## Overview

This library provides a consistent logging interface across all microservices in the MassLab system. It wraps `ILogger<T>` from Microsoft.Extensions.Logging with a custom `ILoggerAdapter<T>` interface that provides a standardized API for logging operations.

## Features

- **ILoggerAdapter<T>**: Consistent logging interface with methods for Information, Warning, Error, and Debug logging
- **LoggingOptions**: Configuration class for logging settings (minimum level, sinks, etc.)
- **Service Registration**: Extension methods for easy DI registration

## Usage

### 1. Add to your project

```xml
<ItemGroup>
  <ProjectReference Include="..\..\common\MassLab.Common.Logging\MassLab.Common.Logging.csproj" />
</ItemGroup>
```

### 2. Configure in appsettings.json

```json
{
  "Logging": {
    "MinimumLevel": "Information",
    "MinimumLevelOverrides": {
      "Microsoft": "Warning",
      "System": "Warning"
    },
    "EnableConsole": true,
    "EnableFile": false,
    "FilePath": "logs/app.log",
    "EnableSeq": false,
    "SeqUrl": "http://localhost:5341",
    "EnableApplicationInsights": false,
    "ApplicationInsightsKey": ""
  }
}
```

### 3. Register services in Program.cs

```csharp
builder.Services.AddCommonLogging(builder.Configuration);
```

### 4. Inject and use in your classes

```csharp
public class MyService
{
    private readonly ILoggerAdapter<MyService> _logger;

    public MyService(ILoggerAdapter<MyService> logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        _logger.LogInformation("Starting work");
        
        try
        {
            // Do work
            _logger.LogDebug("Work in progress");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during work");
            throw;
        }
        
        _logger.LogInformation("Work completed");
    }
}
```

## Integration with Serilog

For Serilog integration with enrichers and advanced sinks, use the `MassLab.Common.Logging.Serilog` library which builds on top of this library.

## Requirements

- .NET 8.0 or later
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.Configuration.Abstractions
- Microsoft.Extensions.DependencyInjection.Abstractions

## License

Internal use only - MassLab project.
