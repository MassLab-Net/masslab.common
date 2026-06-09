# MassLab.Common.HttpClient

Use this for outbound HTTP clients that need API key, JWT, tenant, trace id propagation, logging handlers, or Polly policies.

NuGet:

```bash
dotnet add package MassLab.Common.HttpClient
```

Typical components:

```csharp
ApiKeyDelegatingHandler
JwtPropagationDelegatingHandler
TenantPropagationDelegatingHandler
TraceIdDelegatingHandler
LoggingDelegatingHandler
PollyPolicies
```

Use this with `IHttpClientFactory` registrations in the consuming service.

Read deeper only when needed: `MassLab.Common.HttpClient/README.md`.

