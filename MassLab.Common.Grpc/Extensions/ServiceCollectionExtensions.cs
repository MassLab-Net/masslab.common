using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.Grpc.Interceptors;

namespace MassLab.Common.Grpc.Extensions;

/// <summary>
/// Extension methods for registering gRPC services with tracing, reflection
/// and health-check support.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a gRPC client with automatic traceId propagation.
    /// </summary>
    public static IServiceCollection AddGrpcClientWithTracing<TClient>(
        this IServiceCollection services,
        string serviceName,
        Action<GrpcClientFactoryOptions> configureClient,
        bool enableApiKey = false,
        string? apiKeyClientName = null)
        where TClient : ClientBase<TClient>
    {
        services.AddHttpContextAccessor();
        var clientBuilder = services.AddGrpcClient<TClient>(serviceName, configureClient)
            .AddInterceptor<TraceIdClientInterceptor>();

        if (enableApiKey)
        {
            clientBuilder.AddInterceptor(sp =>
            {
                var interceptor = ActivatorUtilities.CreateInstance<ApiKeyClientInterceptor>(sp);
                interceptor.ApiKeyClientName = apiKeyClientName;
                return interceptor;
            });
        }

        services.AddSingleton<TraceIdClientInterceptor>();

        return services;
    }

    /// <summary>
    /// Adds gRPC server services with automatic traceId extraction.
    /// </summary>
    public static IServiceCollection AddGrpcServerWithTracing(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<TraceIdServerInterceptor>();
        });
        services.AddSingleton<TraceIdServerInterceptor>();
        return services;
    }

    /// <summary>
    /// Registers gRPC server reflection so tools like <c>grpcurl</c> /
    /// <c>Postman</c> can discover services at runtime.
    /// </summary>
    public static IServiceCollection AddMassLabGrpcReflection(this IServiceCollection services)
    {
        services.AddGrpcReflection();
        return services;
    }

    /// <summary>
    /// Registers the gRPC health-checks service.
    /// </summary>
    public static IServiceCollection AddMassLabGrpcHealthChecks(this IServiceCollection services)
    {
        services.AddGrpcHealthChecks();
        return services;
    }
}

/// <summary>
/// Endpoint mapping helpers for gRPC reflection / health.
/// </summary>
public static class GrpcEndpointRouteBuilderExtensions
{
    /// <summary>Maps the gRPC reflection service.</summary>
    public static IEndpointRouteBuilder MapMassLabGrpcReflection(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcReflectionService();
        return endpoints;
    }

    /// <summary>Maps the gRPC health-check service.</summary>
    public static IEndpointRouteBuilder MapMassLabGrpcHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcHealthChecksService();
        return endpoints;
    }
}
