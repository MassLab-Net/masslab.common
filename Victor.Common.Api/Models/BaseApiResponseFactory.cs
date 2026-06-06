using Microsoft.AspNetCore.Http;

namespace Victor.Common.Api.Models;

/// <summary>
/// Factory for building <see cref="BaseApiResponse"/> envelopes with traceId
/// and version resolved from the current <see cref="HttpContext"/> via DI.
/// This is the preferred entry point for new code.
/// </summary>
public class BaseApiResponseFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="BaseApiResponseFactory"/>.
    /// </summary>
    public BaseApiResponseFactory(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        // Wire ambient resolver for any legacy code calling the obsolete static helpers.
        AmbientResolver.Attach(httpContextAccessor);
    }

    /// <summary>Creates a successful response.</summary>
    public BaseApiResponse Success(object? data = null) =>
        BaseApiResponse.Ok(data, ResolveTraceId(), ResolveApiVersion());

    /// <summary>Creates a successful response with typed data.</summary>
    public BaseApiResponse<T> Success<T>(T? data = default) =>
        new() { IsSuccess = true, Data = data, TraceId = ResolveTraceId(), Version = ResolveApiVersion() };

    /// <summary>Creates a failed response.</summary>
    public BaseApiResponse Failure(string message, string code = "ERROR",
        IDictionary<string, string[]>? fields = null) =>
        BaseApiResponse.Fail(message, code, fields, ResolveTraceId(), ResolveApiVersion());

    /// <summary>Creates a failed response with typed data slot.</summary>
    public BaseApiResponse<T> Failure<T>(string message, string code = "ERROR",
        IDictionary<string, string[]>? fields = null) =>
        new()
        {
            IsSuccess = false,
            Data = default,
            Error = new ApiError { Code = code, Message = message, Fields = fields },
            TraceId = ResolveTraceId(),
            Version = ResolveApiVersion(),
        };

    private string ResolveTraceId() => ApiResponseMetadataResolver.ResolveTraceId(_httpContextAccessor.HttpContext);

    private string ResolveApiVersion() => ApiResponseMetadataResolver.ResolveApiVersion(_httpContextAccessor.HttpContext);
}
