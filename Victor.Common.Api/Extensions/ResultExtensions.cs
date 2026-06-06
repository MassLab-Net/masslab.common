using Victor.Common.Api.Models;

namespace Victor.Common.Api.Extensions;

/// <summary>
/// Extension methods for converting Result objects to API responses.
/// </summary>
/// <remarks>
/// These helpers are intentionally backward-compatible with the static
/// <see cref="BaseApiResponse"/> shortcuts. New code should inject
/// <see cref="BaseApiResponseFactory"/> directly.
/// </remarks>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Result"/> to a <see cref="BaseApiResponse"/>.
    /// </summary>
    public static BaseApiResponse ToApiResponse(this Result result, string traceId = "")
    {
#pragma warning disable CS0618 // legacy ambient resolver path, kept for backward compat
        return result.IsSuccess
            ? BaseApiResponse.Success(null, traceId)
            : BaseApiResponse.Failure(result.Error, "OPERATION_FAILED", traceId);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Converts a <see cref="Result{T}"/> to a <see cref="BaseApiResponse{T}"/>.
    /// </summary>
    public static BaseApiResponse<T> ToApiResponse<T>(this Result<T> result, string traceId = "")
    {
#pragma warning disable CS0618
        return result.IsSuccess
            ? BaseApiResponse<T>.Success(result.Value, traceId)
            : BaseApiResponse<T>.Failure(result.Error, "OPERATION_FAILED", traceId);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Converts a <see cref="Result"/> to a <see cref="BaseApiResponse"/>
    /// using the DI-registered <see cref="BaseApiResponseFactory"/>.
    /// </summary>
    public static BaseApiResponse ToApiResponse(this Result result, BaseApiResponseFactory factory) =>
        result.IsSuccess
            ? factory.Success(null)
            : factory.Failure(result.Error, "OPERATION_FAILED");

    /// <summary>
    /// Converts a <see cref="Result{T}"/> to a <see cref="BaseApiResponse{T}"/>
    /// using the DI-registered <see cref="BaseApiResponseFactory"/>.
    /// </summary>
    public static BaseApiResponse<T> ToApiResponse<T>(this Result<T> result, BaseApiResponseFactory factory) =>
        result.IsSuccess
            ? factory.Success<T>(result.Value)
            : factory.Failure<T>(result.Error, "OPERATION_FAILED");
}
