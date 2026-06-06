using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MassLab.Common.Api.Configuration;
using MassLab.Common.Api.Exceptions;
using MassLab.Common.Api.Models;
using MassLab.Common.Validation.Exceptions;

namespace MassLab.Common.Api.Middleware;

/// <summary>
/// Middleware that handles all unhandled exceptions and returns standardized
/// error responses. The shape (<c>BaseApiResponse</c> envelope or RFC 7807
/// <c>ProblemDetails</c>) is controlled by <see cref="ApiResponseOptions"/>.
/// </summary>
public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly ApiResponseOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionMiddleware"/> class.
    /// </summary>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment,
        IOptions<ApiResponseOptions>? options = null)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _options = options?.Value ?? new ApiResponseOptions();
    }

    /// <summary>Invokes the middleware to handle exceptions.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.Items["TraceId"]?.ToString()
                      ?? Activity.Current?.Id
                      ?? context.TraceIdentifier;

        var (statusCode, errorCode, message, fields) = MapException(exception);

        context.Response.StatusCode = statusCode;

        if (_options.Format == ApiResponseFormat.ProblemDetails)
        {
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetailsResponse
            {
                Type = $"https://httpstatuses.io/{statusCode}",
                Title = ReasonPhrase(statusCode),
                Status = statusCode,
                Detail = message,
                Instance = context.Request.Path,
                TraceId = traceId,
                Errors = fields,
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
        else
        {
            context.Response.ContentType = "application/json";
            var envelope = BaseApiResponse.Fail(
                message,
                errorCode,
                fields,
                traceId,
                ApiResponseMetadataResolver.ResolveApiVersion(context));
            await context.Response.WriteAsJsonAsync(envelope);
        }
    }

    private (int Status, string Code, string Message, IDictionary<string, string[]>? Fields)
        MapException(Exception exception) => exception switch
    {
        ValidationException vEx => (
            StatusCodes.Status400BadRequest,
            "VALIDATION_ERROR",
            "One or more validation errors occurred.",
            vEx.Errors),

        NotFoundException nfEx => (
            StatusCodes.Status404NotFound,
            "NOT_FOUND",
            nfEx.Message,
            null),

        UnauthorizedException uEx => (
            StatusCodes.Status401Unauthorized,
            "UNAUTHORIZED",
            uEx.Message,
            null),

        ForbiddenException fEx => (
            StatusCodes.Status403Forbidden,
            "FORBIDDEN",
            fEx.Message,
            null),

        ConflictException cEx => (
            StatusCodes.Status409Conflict,
            "CONFLICT",
            cEx.Message,
            null),

        _ => (
            StatusCodes.Status500InternalServerError,
            "INTERNAL_SERVER_ERROR",
            (_environment.IsDevelopment() || _options.IncludeExceptionDetails)
                ? exception.Message
                : "An error occurred processing your request.",
            null),
    };

    private static string ReasonPhrase(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        500 => "Internal Server Error",
        _   => "Error",
    };
}
