using Grpc.Core;
using Victor.Common.Api.Exceptions;
using Victor.Common.Validation.Exceptions;

namespace Victor.Common.Grpc.ErrorHandling;

/// <summary>
/// Handles gRPC exceptions and maps them to application-specific exceptions.
/// </summary>
public static class GrpcExceptionHandler
{
    /// <summary>
    /// Maps a gRPC RpcException to an appropriate application exception.
    /// </summary>
    /// <param name="rpcException">The RpcException to handle.</param>
    /// <returns>An application-specific exception based on the gRPC status code.</returns>
    public static Exception HandleGrpcException(RpcException rpcException)
    {
        return rpcException.StatusCode switch
        {
            StatusCode.NotFound => new NotFoundException(rpcException.Status.Detail),
            StatusCode.InvalidArgument => new ValidationException(),
            StatusCode.Unauthenticated => new UnauthorizedException(rpcException.Status.Detail),
            StatusCode.PermissionDenied => new ForbiddenException(rpcException.Status.Detail),
            _ => new InvalidOperationException($"gRPC call failed: {rpcException.Status.Detail}", rpcException)
        };
    }
}
