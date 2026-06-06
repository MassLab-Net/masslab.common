namespace Victor.Common.Api.Configuration;

/// <summary>
/// Choice of error-response shape returned by <c>GlobalExceptionMiddleware</c>.
/// </summary>
public enum ApiResponseFormat
{
    /// <summary>Use the <c>BaseApiResponse</c> envelope (default — backward compatible).</summary>
    Envelope = 0,

    /// <summary>Use RFC 7807 <c>ProblemDetailsResponse</c> (machine-friendly).</summary>
    ProblemDetails = 1,
}

/// <summary>
/// Options controlling the API response envelope and error formatting.
/// </summary>
public class ApiResponseOptions
{
    /// <summary>
    /// Configuration section name (<c>ApiResponse</c>).
    /// </summary>
    public const string SectionName = "ApiResponse";

    /// <summary>
    /// Shape of the error response. Defaults to <see cref="ApiResponseFormat.Envelope"/>.
    /// </summary>
    public ApiResponseFormat Format { get; set; } = ApiResponseFormat.Envelope;

    /// <summary>
    /// When <c>true</c>, includes detailed exception messages in non-development
    /// environments. Defaults to <c>false</c> (recommended for production).
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = false;
}
