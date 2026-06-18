using System.Text.RegularExpressions;
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
/// Service-collection and application-builder extensions for rate limiting.
/// </summary>
public static class RateLimitingExtensions
{
    public const string DefaultPolicyName = "masslab-default";

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
            // Global limiter - applies to all requests
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var opts = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                var (partitionKey, policy) = ResolveClientPolicy(httpContext, opts);
                return CreateLimiter(partitionKey, opts, policy);
            });

            // Default named policy
            options.AddPolicy(DefaultPolicyName, httpContext =>
            {
                var opts = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                var (partitionKey, policy) = ResolveClientPolicy(httpContext, opts);
                return CreateLimiter(partitionKey, opts, policy);
            });

            // Named policies from config
            foreach (var policyName in configuredPolicyNames)
            {
                options.AddPolicy(policyName, httpContext =>
                {
                    var opts = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                    var activePolicy = opts.Policies.GetValueOrDefault(policyName) ?? new RateLimitPolicyOptions();
                    var partitionKey = ResolveNamedPolicyPartitionKey(httpContext, policyName, activePolicy, opts);
                    return CreateLimiter(partitionKey, opts, activePolicy);
                });
            }

            options.OnRejected = async (ctx, ct) =>
            {
                var opts = ctx.HttpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                ctx.HttpContext.Response.StatusCode = opts.RejectionStatusCode;
                
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    ctx.HttpContext.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();
                
                ctx.HttpContext.Response.Headers["X-RateLimit-Limit"] = opts.PermitLimit.ToString();
                ctx.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                await ctx.HttpContext.Response.WriteAsync("Too many requests.", ct);
            };
        });

        return services;
    }

    /// <summary>
    /// Resolves client-specific policy based on PartitionBy setting.
    /// </summary>
    private static (string PartitionKey, RateLimitPolicyOptions? Policy) ResolveClientPolicy(
        HttpContext ctx, RateLimitingOptions opts)
    {
        var endpoint = $"{ctx.Request.Method}:{ctx.Request.Path}";
        var isUserPartition = string.Equals(opts.PartitionBy, "user", StringComparison.OrdinalIgnoreCase);

        if (isUserPartition)
        {
            var userId = ResolveUserId(ctx, opts);
            if (!string.IsNullOrEmpty(userId))
            {
                var clientPolicy = FindClientPolicy(opts.UserPartition?.Policies, userId, opts.UserPartition?.DefaultPolicy);
                var effectivePolicy = ResolveEndpointPolicy(clientPolicy, ctx.Request.Path);
                var perEndpoint = effectivePolicy?.PerEndpoint ?? opts.PerEndpoint;
                var key = perEndpoint ? $"user:{userId}:{endpoint}" : $"user:{userId}";
                return (key, effectivePolicy);
            }
        }

        // IP partition or fallback
        var ip = ResolveIp(ctx);
        var ipClientPolicy = FindClientPolicy(opts.IpPartition?.Policies, ip, opts.IpPartition?.DefaultPolicy);
        var ipEffectivePolicy = ResolveEndpointPolicy(ipClientPolicy, ctx.Request.Path);
        var ipPerEndpoint = ipEffectivePolicy?.PerEndpoint ?? opts.PerEndpoint;
        var ipKey = ipPerEndpoint ? $"ip:{ip}:{endpoint}" : $"ip:{ip}";
        return (ipKey, ipEffectivePolicy);
    }

    /// <summary>
    /// Finds matching client policy by exact match or wildcard.
    /// </summary>
    private static ClientRateLimitPolicy? FindClientPolicy(
        Dictionary<string, ClientRateLimitPolicy>? policies, string clientId, ClientRateLimitPolicy? defaultPolicy)
    {
        if (policies == null || policies.Count == 0) return null;

        // Exact match first
        if (policies.TryGetValue(clientId, out var exact)) return exact;

        // Wildcard match (e.g., "10.0.0.*" matches "10.0.0.123")
        foreach (var (pattern, policy) in policies)
        {
            if (MatchesWildcard(pattern, clientId)) return policy;
        }

        return defaultPolicy;
    }

    /// <summary>
    /// Resolves endpoint-specific policy or falls back to defaults.
    /// </summary>
    private static RateLimitPolicyOptions? ResolveEndpointPolicy(
        ClientRateLimitPolicy? clientPolicy, PathString path)
    {
        // Priority: EndpointOverride > ClientPolicy.DefaultLimit > PartitionDefault
        if (clientPolicy != null)
        {
            // Check endpoint overrides with wildcard support
            foreach (var (pattern, policy) in clientPolicy.EndpointOverrides)
            {
                if (MatchesWildcard(pattern, path.Value ?? "")) return policy;
            }

            // Fallback to client's default
            if (clientPolicy.DefaultLimit != null) return clientPolicy.DefaultLimit;
        }
        return null;
    }

    /// <summary>
    /// Resolves partition key for named policies ([EnableRateLimiting]).
    /// </summary>
    private static string ResolveNamedPolicyPartitionKey(
        HttpContext ctx, string policyName, RateLimitPolicyOptions policy, RateLimitingOptions opts)
    {
        var endpoint = $"{ctx.Request.Method}:{ctx.Request.Path}";
        var perEndpoint = policy.PerEndpoint ?? opts.PerEndpoint;

        return policy.PartitionBy.ToLowerInvariant() switch
        {
            "endpoint" => $"policy:{policyName}:{endpoint}",
            "user" => BuildKey("user", ResolveUserId(ctx, opts) ?? ResolveIp(ctx), endpoint, perEndpoint),
            _ => BuildKey("ip", ResolveIp(ctx), endpoint, perEndpoint)
        };
    }

    private static string BuildKey(string prefix, string id, string endpoint, bool perEndpoint)
        => perEndpoint ? $"{prefix}:{id}:{endpoint}" : $"{prefix}:{id}";

    private static string? ResolveUserId(HttpContext ctx, RateLimitingOptions opts)
    {
        var claimName = opts.UserPartition?.ClaimName ?? "sub";
        var fallback = opts.UserPartition?.FallbackClaimName ?? System.Security.Claims.ClaimTypes.NameIdentifier;
        
        var userId = ctx.Items["ClientId"] as string ?? ctx.User.FindFirst(claimName)?.Value ?? ctx.User.FindFirst(fallback)?.Value;
        return userId;
    }

    private static string ResolveIp(HttpContext ctx)
        => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>
    /// Matches value against a wildcard pattern. Supports "*" for any characters.
    /// </summary>
    private static bool MatchesWildcard(string pattern, string value)
    {
        if (string.Equals(pattern, value, StringComparison.OrdinalIgnoreCase)) return true;
        if (!pattern.Contains('*')) return false;

        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
    }

    private static RateLimitPartition<string> CreateLimiter(
        string partitionKey, RateLimitingOptions opts, RateLimitPolicyOptions? policy)
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
            throw new ArgumentOutOfRangeException(nameof(options.PermitLimit));
        if (options.WindowSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.WindowSeconds));
        if (options.QueueLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(options.QueueLimit));
        if (options.RejectionStatusCode < 400 || options.RejectionStatusCode > 599)
            throw new ArgumentOutOfRangeException(nameof(options.RejectionStatusCode));

        var validGlobalPartitions = new[] { "user", "ip" };
        if (!validGlobalPartitions.Contains(options.PartitionBy.ToLowerInvariant()))
            throw new ArgumentException("PartitionBy must be 'user' or 'ip'.");

        var validPolicyPartitions = new[] { "user", "ip", "endpoint" };
        foreach (var (_, policy) in options.Policies)
        {
            if (!validPolicyPartitions.Contains(policy.PartitionBy.ToLowerInvariant()))
                throw new ArgumentException(
                    $"PartitionBy must be 'user', 'ip', or 'endpoint'.",
                    nameof(RateLimitPolicyOptions.PartitionBy));
        }
    }

    public static IApplicationBuilder UseMassLabRateLimiting(this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }
}