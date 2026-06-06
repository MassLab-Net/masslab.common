using Microsoft.AspNetCore.Http;

namespace MassLab.Common.Api.Middleware;

/// <summary>
/// Options for <see cref="SecurityHeadersMiddleware"/>.
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>Adds <c>Strict-Transport-Security</c> for ≥ 1 year, includeSubDomains.</summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>Adds <c>X-Content-Type-Options: nosniff</c>.</summary>
    public bool EnableContentTypeOptions { get; set; } = true;

    /// <summary>Adds <c>X-Frame-Options: DENY</c>.</summary>
    public bool EnableFrameOptions { get; set; } = true;

    /// <summary>Adds <c>Referrer-Policy: strict-origin-when-cross-origin</c>.</summary>
    public bool EnableReferrerPolicy { get; set; } = true;

    /// <summary>Adds <c>X-XSS-Protection: 0</c> (modern best practice — disables legacy XSS filter).</summary>
    public bool EnableXssProtection { get; set; } = true;

    /// <summary>Adds <c>Permissions-Policy</c> header to disable geolocation, microphone, camera by default.</summary>
    public bool EnablePermissionsPolicy { get; set; } = true;
}

/// <summary>
/// Middleware that appends standard security response headers (HSTS,
/// X-Content-Type-Options, X-Frame-Options, Referrer-Policy, etc.).
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="SecurityHeadersMiddleware"/>.
    /// </summary>
    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions? options = null)
    {
        _next = next;
        _options = options ?? new SecurityHeadersOptions();
    }

    /// <summary>Invokes the middleware to attach security headers.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            if (_options.EnableHsts && context.Request.IsHttps)
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            if (_options.EnableContentTypeOptions)
                headers["X-Content-Type-Options"] = "nosniff";

            if (_options.EnableFrameOptions)
                headers["X-Frame-Options"] = "DENY";

            if (_options.EnableReferrerPolicy)
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            if (_options.EnableXssProtection)
                headers["X-XSS-Protection"] = "0";

            if (_options.EnablePermissionsPolicy)
                headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
