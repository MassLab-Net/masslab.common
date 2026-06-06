# MassLab.Common.Grpc

Common gRPC library providing abstractions, interceptors, and error handling for internal microservice communication with automatic traceId propagation.

## Features

- **gRPC Client Abstractions**: Factory interface for creating gRPC clients
- **gRPC Service Base**: Common interface for gRPC services
- **TraceId Propagation**: Automatic traceId injection and extraction for distributed tracing
- **API Key Propagation**: Optional API key metadata for service-to-service or external calls
- **Error Handling**: Maps gRPC status codes to application exceptions
- **Easy Registration**: Extension methods for DI registration

## Installation

Add a project reference to `MassLab.Common.Grpc`:

```xml
<ProjectReference Include="..\..\common\MassLab.Common.Grpc\MassLab.Common.Grpc.csproj" />
```

## Usage

### Server-Side Setup

Register gRPC server with tracing in `Program.cs`:

```csharp
using MassLab.Common.Grpc.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC server with automatic traceId extraction
builder.Services.AddGrpcServerWithTracing();

var app = builder.Build();

// Map your gRPC services
app.MapGrpcService<YourGrpcService>();

app.Run();
```

### Client-Side Setup

Register gRPC client with tracing in `Program.cs`:

```csharp
using MassLab.Common.Grpc.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC client with automatic traceId propagation
builder.Services.AddGrpcClientWithTracing<YourGrpcClient>(
    "YourService",
    options =>
    {
        options.Address = new Uri("https://your-service-url");
    },
    enableApiKey: true,
    apiKeyClientName: "product-api");

var app = builder.Build();
app.Run();
```

When `enableApiKey` is true, the client interceptor adds:

```text
x-api-key: <ApiKey:Clients:product-api:ApiKey>
x-service-name: <ApiKey:Clients:product-api:ServiceName>
```

Server-side gRPC services can use the same API key authentication scheme
because gRPC metadata is available as request headers:

```csharp
[Authorize(AuthenticationSchemes = ApiKeyDefaults.AuthenticationScheme)]
public class ProductGrpcService : ProductGrpc.ProductGrpcBase
{
}
```

### Implementing a gRPC Service

Read the propagated trace id from `HttpContext.Items["TraceId"]` when needed:

```csharp
using Grpc.Core;

public class YourGrpcService : YourService.YourServiceBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public YourGrpcService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetTraceId()
    {
        return _httpContextAccessor.HttpContext?.Items["TraceId"]?.ToString() 
            ?? Activity.Current?.Id 
            ?? Guid.NewGuid().ToString();
    }

    public override async Task<YourResponse> YourMethod(
        YourRequest request, 
        ServerCallContext context)
    {
        var traceId = GetTraceId();
        // Your implementation
    }
}
```

### Handling gRPC Exceptions

Use `GrpcExceptionHandler` to map gRPC exceptions:

```csharp
using MassLab.Common.Grpc.ErrorHandling;
using Grpc.Core;

try
{
    var response = await grpcClient.YourMethodAsync(request);
}
catch (RpcException rpcEx)
{
    var appException = GrpcExceptionHandler.HandleGrpcException(rpcEx);
    throw appException;
}
```

## gRPC Status Code Mapping

The library maps gRPC status codes to application exceptions:

| gRPC Status Code | Application Exception |
|-----------------|----------------------|
| `NotFound` | `NotFoundException` |
| `InvalidArgument` | `ValidationException` |
| `Unauthenticated` | `UnauthorizedException` |
| `PermissionDenied` | `ForbiddenException` |
| Others | `InvalidOperationException` |

## TraceId Propagation

### Client Interceptor

The `TraceIdClientInterceptor` automatically:
1. Extracts traceId from `HttpContext.Items["TraceId"]` or `Activity.Current?.Id`
2. Adds `x-trace-id` metadata to outgoing gRPC calls

### Server Interceptor

The `TraceIdServerInterceptor` automatically:
1. Extracts `x-trace-id` from incoming gRPC call metadata
2. Stores traceId in `HttpContext.Items["TraceId"]`

This enables end-to-end distributed tracing across microservices.

## Dependencies

- `Grpc.AspNetCore` (2.60.0)
- `Grpc.Net.Client` (2.60.0)
- `MassLab.Common.Api` (for exception types)
- `MassLab.Common.Validation` (for ValidationException)

## Integration with Other MassLab Libraries

This library integrates seamlessly with:
- **MassLab.Common.Api**: Uses exception types for error handling
- **MassLab.Common.Logging.Serilog**: TraceId is automatically enriched in logs
- **MassLab.Common.Validation**: ValidationException mapping for invalid arguments
