using Microsoft.AspNetCore.Http;

namespace Victor.Common.Api.Models;

/// <summary>
/// Represents a standardized API response envelope.
/// </summary>
public class BaseApiResponse
{
    /// <summary>Indicates whether the operation was successful.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>The response data (null if operation failed).</summary>
    public object? Data { get; set; }

    /// <summary>Error details if the operation failed (null if successful).</summary>
    public ApiError? Error { get; set; }

    /// <summary>The trace identifier for correlation across distributed systems.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>API version.</summary>
    public string Version { get; set; } = "1.0.0";

    // ─── DI-friendly entry points (preferred) ────────────────────────────────

    /// <summary>
    /// Creates a successful response. Trace-id and version are resolved by the
    /// caller (typically <see cref="BaseApiResponseFactory"/>).
    /// </summary>
    internal static BaseApiResponse Ok(object? data, string traceId, string version) =>
        new() { IsSuccess = true, Data = data, TraceId = traceId, Version = version };

    /// <summary>
    /// Creates a failed response. Trace-id and version are resolved by the
    /// caller (typically <see cref="BaseApiResponseFactory"/>).
    /// </summary>
    internal static BaseApiResponse Fail(string message, string code, IDictionary<string, string[]>? fields,
        string traceId, string version) =>
        new()
        {
            IsSuccess = false,
            Error = new ApiError { Code = code, Message = message, Fields = fields },
            TraceId = traceId,
            Version = version,
        };

    // ─── Backward-compatible static helpers (used by legacy code) ────────────
    // These rely on a static accessor populated by UseGlobalExceptionHandler();
    // when no accessor is available they fall back to ambient values.

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="BaseApiResponseFactory.Success"/> via DI in new code.
    /// This static helper relies on an ambient <see cref="IHttpContextAccessor"/>
    /// populated by <c>UseGlobalExceptionHandler()</c>.
    /// </remarks>
    [Obsolete("Inject BaseApiResponseFactory and call Success(...) instead.")]
    public static BaseApiResponse Success(object? data = null, string traceId = "")
    {
        return new BaseApiResponse
        {
            IsSuccess = true,
            Data = data,
            Error = null,
            TraceId = string.IsNullOrEmpty(traceId) ? AmbientResolver.ResolveTraceId() : traceId,
            Version = AmbientResolver.ResolveVersion(),
        };
    }

    /// <summary>
    /// Creates a failed response with error details.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="BaseApiResponseFactory.Failure"/> via DI in new code.
    /// </remarks>
    [Obsolete("Inject BaseApiResponseFactory and call Failure(...) instead.")]
    public static BaseApiResponse Failure(string message, string code = "ERROR", string traceId = "")
    {
        return new BaseApiResponse
        {
            IsSuccess = false,
            Data = null,
            Error = new ApiError { Code = code, Message = message },
            TraceId = string.IsNullOrEmpty(traceId) ? AmbientResolver.ResolveTraceId() : traceId,
            Version = AmbientResolver.ResolveVersion(),
        };
    }
}

/// <summary>
/// Represents a generic API response with typed data.
/// </summary>
/// <typeparam name="T">The type of the response data.</typeparam>
public class BaseApiResponse<T>
{
    /// <summary>Indicates whether the operation was successful.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>The response data (null if operation failed).</summary>
    public T? Data { get; set; }

    /// <summary>Error details if the operation failed (null if successful).</summary>
    public ApiError? Error { get; set; }

    /// <summary>The trace identifier for correlation across distributed systems.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>API version.</summary>
    public string Version { get; set; } = "1.0.0";

    [Obsolete("Inject BaseApiResponseFactory and call Success<T>(...) instead.")]
    public static BaseApiResponse<T> Success(T? data = default, string traceId = "")
    {
        return new BaseApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Error = null,
            TraceId = string.IsNullOrEmpty(traceId) ? AmbientResolver.ResolveTraceId() : traceId,
            Version = AmbientResolver.ResolveVersion(),
        };
    }

    [Obsolete("Inject BaseApiResponseFactory and call Failure<T>(...) instead.")]
    public static BaseApiResponse<T> Failure(string message, string code = "ERROR", string traceId = "")
    {
        return new BaseApiResponse<T>
        {
            IsSuccess = false,
            Data = default,
            Error = new ApiError { Code = code, Message = message },
            TraceId = string.IsNullOrEmpty(traceId) ? AmbientResolver.ResolveTraceId() : traceId,
            Version = AmbientResolver.ResolveVersion(),
        };
    }
}

/// <summary>
/// Represents error details in an API response.
/// </summary>
public class ApiError
{
    /// <summary>The error code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Field-level validation errors (only populated for validation failures).
    /// Keys are property names; values are arrays of error messages.
    /// </summary>
    public IDictionary<string, string[]>? Fields { get; set; }
}

/// <summary>
/// Internal helper that supplies trace-id / version to the obsolete static
/// helpers. Prefer <see cref="BaseApiResponseFactory"/> via DI.
/// </summary>
internal static class AmbientResolver
{
    private static IHttpContextAccessor? _accessor;

    /// <summary>
    /// Sets the ambient accessor. Called once by
    /// <c>BaseApiResponseFactory.AttachAmbient(...)</c> during DI registration.
    /// </summary>
    internal static void Attach(IHttpContextAccessor accessor) => _accessor = accessor;

    internal static string ResolveTraceId()
    {
        return ApiResponseMetadataResolver.ResolveTraceId(_accessor?.HttpContext);
    }

    internal static string ResolveVersion()
    {
        return ApiResponseMetadataResolver.ResolveApiVersion(_accessor?.HttpContext);
    }
}
