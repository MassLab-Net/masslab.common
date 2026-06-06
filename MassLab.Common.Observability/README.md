# MassLab.Common.Observability

OpenTelemetry registration for traces and metrics, plus a Prometheus scrape
endpoint.

## Program.cs

```csharp
using MassLab.Common.Observability.Extensions;

builder.Services.AddMassLabObservability(builder.Configuration);

var app = builder.Build();
app.UseMassLabPrometheus();
```

## Configuration

```json
{
  "Observability": {
    "ServiceName": "ProductApi",
    "ServiceVersion": "1.0.0",
    "Environment": "staging",
    "OtlpEndpoint": "http://otel-collector:4317",
    "EnablePrometheus": true,
    "PrometheusEndpoint": "/metrics"
  }
}
```

## What it instruments

- ASP.NET Core inbound requests
- HttpClient outbound calls
- EF Core database operations
- gRPC clients
- Redis when `IConnectionMultiplexer` is registered

Scrape metrics from `/metrics` unless `PrometheusEndpoint` is changed.
