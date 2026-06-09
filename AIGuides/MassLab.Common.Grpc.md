# MassLab.Common.Grpc

Use this when the service exposes or calls gRPC and needs common interceptors, error handling, API key propagation, trace id propagation, reflection, or health checks.

NuGet:

```bash
dotnet add package MassLab.Common.Grpc
```

Main APIs:

```csharp
builder.Services.AddMassLabGrpcReflection();
builder.Services.AddMassLabGrpcHealthChecks();
```

Common components include `ApiKeyClientInterceptor`, `TraceIdClientInterceptor`, `TraceIdServerInterceptor`, and gRPC exception handling.

Read deeper only when needed: `MassLab.Common.Grpc/README.md`.

