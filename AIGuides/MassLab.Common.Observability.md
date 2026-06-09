# MassLab.Common.Observability

Use this for OpenTelemetry traces, metrics, and Prometheus endpoint.

NuGet:

```bash
dotnet add package MassLab.Common.Observability
```

Setup:

```csharp
builder.Services.AddMassLabObservability(builder.Configuration);

app.UseMassLabPrometheus();
```

Config section: `Observability`.

Instruments ASP.NET Core, HttpClient, EF Core, gRPC clients, and Redis when available.

Read deeper only when needed: `MassLab.Common.Observability/README.md`.

