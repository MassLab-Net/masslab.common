using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MassLab.Common.RateLimiting.Configuration;

namespace MassLab.Common.RateLimiting.Extensions;

/// <summary>
/// Service-collection &amp; application-builder extensions for rate limiting.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>Default policy name registered by <c>AddMassLabRateLimiting</c>.</summary>
    public const string DefaultPolicyName = "masslab-default";

    /// <summary>
    /// Registers a fixed-window rate limiter partitioned by authenticated
    /// user id (falling back to remote IP).
    /// </summary>
    public static IServiceCollection AddMassLabRateLimiting(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = RateLimitingOptions.SectionName)
    {
        var initialOptions = new RateLimitingOptions();
        configuration?.GetSection(sectionName).Bind(initialOptions);
        Validate(initialOptions);

        var configuredPolicyNames = new List<string>();
        if (configuration != null)
        {
            services.Configure<RateLimitingOptions>(configuration.GetSection(sectionName));
            configuredPolicyNames.AddRange(initialOptions.Policies.Keys);
        }
        else
        {
            services.Configure<RateLimitingOptions>(_ => { });
        }

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var sp = httpContext.RequestServices;
                var opts = sp.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                Validate(opts);
                var partitionKey = ResolvePartitionKey(httpContext, opts);
                return CreateLimiter(partitionKey, opts, null);
            });

            options.AddPolicy(DefaultPolicyName, httpContext =>
            {
                var opts = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                Validate(opts);
                var partitionKey = ResolvePartitionKey(httpContext, opts);
                return CreateLimiter(partitionKey, opts, null);
            });

            foreach (var policyName in configuredPolicyNames)
            {
                options.AddPolicy(policyName, httpContext =>
                {
                    var opts = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                    Validate(opts);
                    var activePolicy = opts.Policies.TryGetValue(policyName, out var configured) ? configured : new RateLimitPolicyOptions();
                    var partitionKey = ResolvePolicyPartitionKey(httpContext, policyName, activePolicy, opts);
                    return CreateLimiter(partitionKey, opts, activePolicy);
                });
            }

            options.OnRejected = async (ctx, ct) =>
            {
                var opts = ctx.HttpContext.RequestServices
                    .GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                ctx.HttpContext.Response.StatusCode = opts.RejectionStatusCode;
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    ctx.HttpContext.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();
                if (ctx.Lease.TryGetMetadata(MetadataName.ReasonPhrase, out _))
                {
                    // Attempt to provide limit info from options
                }
                ctx.HttpContext.Response.Headers["X-RateLimit-Limit"] = opts.PermitLimit.ToString();
                ctx.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                await ctx.HttpContext.Response.WriteAsync("Too many requests.", ct);
            };
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext ctx, RateLimitingOptions opts)
    {
        if (opts.UseUserPartitioning)
        {
            var userId = ctx.User.FindFirst("sub")?.Value
                         ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId)) return $"user:{userId}";
        }
        return $"ip:{ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private static string ResolvePolicyPartitionKey(
        HttpContext ctx,
        string policyName,
        RateLimitPolicyOptions policy,
        RateLimitingOptions opts)
    {
        return policy.PartitionBy.ToLowerInvariant() switch
        {
            "endpoint" => $"endpoint:{policyName}:{ctx.Request.Method}:{ctx.Request.Path}",
            "user" => $"user:{ResolveUserId(ctx) ?? ResolveIp(ctx)}",
            _ => opts.UseUserPartitioning && ResolveUserId(ctx) is { Length: > 0 } userId
                ? $"user:{userId}"
                : $"ip:{ResolveIp(ctx)}"
        };
    }

    private static string? ResolveUserId(HttpContext ctx)
        => ctx.User.FindFirst("sub")?.Value
           ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    private static string ResolveIp(HttpContext ctx)
        => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static RateLimitPartition<string> CreateLimiter(
        string partitionKey,
        RateLimitingOptions opts,
        RateLimitPolicyOptions? policy)
    {
        var limiter = policy?.Limiter ?? opts.Limiter;
        var permitLimit = policy?.PermitLimit ?? opts.PermitLimit;
        var queueLimit = policy?.QueueLimit ?? opts.QueueLimit;
        var window = TimeSpan.FromSeconds(policy?.WindowSeconds ?? opts.WindowSeconds);

        return limiter switch
        {
            RateLimiterKind.SlidingWindow => RateLimitPartition.GetSlidingWindowLimiter(partitionKey,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    SegmentsPerWindow = Math.Max(1, policy?.SegmentsPerWindow ?? opts.SegmentsPerWindow),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = queueLimit,
                    AutoReplenishment = true
                }),
            RateLimiterKind.TokenBucket => RateLimitPartition.GetTokenBucketLimiter(partitionKey,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = permitLimit,
                    TokensPerPeriod = policy?.TokensPerPeriod ?? opts.TokensPerPeriod,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(policy?.ReplenishmentSeconds ?? opts.ReplenishmentSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = queueLimit,
                    AutoReplenishment = true
                }),
            _ => RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = queueLimit,
                    AutoReplenishment = true
                })
        };
    }

    private static void Validate(RateLimitingOptions options)
    {
        if (options.PermitLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.PermitLimit), options.PermitLimit, "Permit limit must be greater than zero.");
        if (options.WindowSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.WindowSeconds), options.WindowSeconds, "Window seconds must be greater than zero.");
        if (options.QueueLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(options.QueueLimit), options.QueueLimit, "Queue limit cannot be negative.");
        if (options.RejectionStatusCode < StatusCodes.Status400BadRequest || options.RejectionStatusCode > 599)
            throw new ArgumentOutOfRangeException(nameof(options.RejectionStatusCode), options.RejectionStatusCode, "Rejection status code must be a 4xx or 5xx status code.");
        if (options.SegmentsPerWindow <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.SegmentsPerWindow), options.SegmentsPerWindow, "Segments per window must be greater than zero.");
        if (options.ReplenishmentSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.ReplenishmentSeconds), options.ReplenishmentSeconds, "Replenishment seconds must be greater than zero.");
        if (options.TokensPerPeriod <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.TokensPerPeriod), options.TokensPerPeriod, "Tokens per period must be greater than zero.");

        if (options.Policies is null)
            throw new ArgumentException("Policies collection cannot be null.", nameof(options.Policies));

        foreach (var (name, policy) in options.Policies)
            ValidatePolicy(name, policy);
    }

    private static void ValidatePolicy(string name, RateLimitPolicyOptions policy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Policy name is required.", nameof(name));
        if (string.Equals(name, DefaultPolicyName, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Policy name '{DefaultPolicyName}' is reserved.", nameof(name));
        if (policy.PermitLimit is <= 0)
            throw new ArgumentOutOfRangeException(nameof(policy.PermitLimit), policy.PermitLimit, "Policy permit limit must be greater than zero.");
        if (policy.WindowSeconds is <= 0)
            throw new ArgumentOutOfRangeException(nameof(policy.WindowSeconds), policy.WindowSeconds, "Policy window seconds must be greater than zero.");
        if (policy.QueueLimit is < 0)
            throw new ArgumentOutOfRangeException(nameof(policy.QueueLimit), policy.QueueLimit, "Policy queue limit cannot be negative.");
        if (policy.SegmentsPerWindow is <= 0)
            throw new ArgumentOutOfRangeException(nameof(policy.SegmentsPerWindow), policy.SegmentsPerWindow, "Policy segments per window must be greater than zero.");
        if (policy.ReplenishmentSeconds is <= 0)
            throw new ArgumentOutOfRangeException(nameof(policy.ReplenishmentSeconds), policy.ReplenishmentSeconds, "Policy replenishment seconds must be greater than zero.");
        if (policy.TokensPerPeriod is <= 0)
            throw new ArgumentOutOfRangeException(nameof(policy.TokensPerPeriod), policy.TokensPerPeriod, "Policy tokens per period must be greater than zero.");
        if (!IsSupportedPartition(policy.PartitionBy))
            throw new ArgumentException("PartitionBy must be 'ip', 'user', or 'endpoint'.", nameof(policy.PartitionBy));
    }

    private static bool IsSupportedPartition(string? partitionBy)
        => !string.IsNullOrWhiteSpace(partitionBy)
           && (string.Equals(partitionBy, "ip", StringComparison.OrdinalIgnoreCase)
           || string.Equals(partitionBy, "user", StringComparison.OrdinalIgnoreCase)
           || string.Equals(partitionBy, "endpoint", StringComparison.OrdinalIgnoreCase));

    /// <summary>Mounts the rate-limiter middleware.</summary>
    public static IApplicationBuilder UseMassLabRateLimiting(this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }
}
