# Victor.Common.Logging.Serilog

Serilog integration library for structured logging with traceId enrichment.

## Features

- **TraceId Enrichment**: Automatically enriches all logs with traceId from HttpContext or Activity
- **Multiple Sinks**: Supports Console, File, Seq, and Application Insights sinks
- **Configurable**: Uses LoggingOptions from Victor.Common.Logging for configuration
- **Structured Logging**: Provides structured logging with properties for queryable logs

## Usage

### Configuration

Add the following to your `appsettings.json`:

```json
{
  "Logging": {
    "MinimumLevel": "Information",
    "MinimumLevelOverrides": {
      "Microsoft": "Warning",
      "System": "Warning"
    },
    "EnableConsole": true,
    "EnableFile": true,
    "FilePath": "logs/app-.log",
    "EnableSeq": false,
    "SeqUrl": "http://localhost:5341",
    "EnableApplicationInsights": false,
    "ApplicationInsightsKey": ""
  }
}
```

### Registration

In your `Program.cs`:

```csharp
using Victor.Common.Logging.Serilog.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog logging
builder.Services.AddSerilogLogging(builder.Configuration);

var app = builder.Build();
app.Run();
```

## TraceId Enrichment

The `TraceIdEnricher` automatically extracts traceId from:
1. `HttpContext.Items["TraceId"]` (set by TraceIdMiddleware)
2. `Activity.Current?.Id` (fallback)

All logs will include the traceId property for distributed tracing.

## Output Templates

### Console Sink
```
[HH:mm:ss Level] TraceId Message
```

### File Sink
```
[yyyy-MM-dd HH:mm:ss.fff zzz] [Level] TraceId Message
```

## Dependencies

- Serilog
- Serilog.AspNetCore
- Serilog.Sinks.Console
- Serilog.Sinks.File
- Serilog.Sinks.Seq
- Serilog.Sinks.ApplicationInsights
- Victor.Common.Logging
