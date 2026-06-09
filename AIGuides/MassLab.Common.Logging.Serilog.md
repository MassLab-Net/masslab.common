# MassLab.Common.Logging.Serilog

Use this for Serilog structured logging with trace id enrichment and configurable sinks.

NuGet:

```bash
dotnet add package MassLab.Common.Logging.Serilog
```

Setup:

```csharp
builder.Services.AddSerilogLogging(builder.Configuration);
```

Config section: `Logging`.

Supports console, file, Seq, Application Insights, and trace id enrichment from `HttpContext` or `Activity`.

Read deeper only when needed: `MassLab.Common.Logging.Serilog/README.md`.

