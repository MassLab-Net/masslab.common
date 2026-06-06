using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MassLab.Common.Caching.Abstractions;
using MassLab.Common.Caching.Models;
using MassLab.Common.Idempotency.Configuration;
using MassLab.Common.Idempotency.Models;

namespace MassLab.Common.Idempotency.Middleware;

/// <summary>Middleware that caches successful write responses by Idempotency-Key.</summary>
public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Creates the middleware.</summary>
    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Runs the middleware.</summary>
    public async Task InvokeAsync(
        HttpContext context,
        ICacheService cache,
        IOptions<IdempotencyOptions> optionsAccessor,
        IDistributedLock? distributedLock = null)
    {
        var options = optionsAccessor.Value;
        if (!options.Methods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var key = context.Request.Headers[options.HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            if (options.RequireHeader)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync($"{options.HeaderName} header is required.", context.RequestAborted);
                return;
            }

            await _next(context);
            return;
        }

        var cacheKey = $"{options.CacheKeyPrefix}:{context.Request.Method}:{context.Request.Path}:{key}";
        var cached = await cache.GetAsync<IdempotencyCacheEntry>(cacheKey, context.RequestAborted);
        if (cached is not null)
        {
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            await context.Response.Body.WriteAsync(cached.Body, context.RequestAborted);
            return;
        }

        LockToken? lockToken = null;
        if (distributedLock is not null)
        {
            lockToken = await distributedLock.AcquireLockAsync(
                cacheKey,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(30),
                context.RequestAborted);

            if (lockToken is null)
            {
                if (await TryReplayCachedResponseAsync(context, cache, cacheKey, originalBody: null))
                    return;

                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsync("A request with the same idempotency key is already processing.", context.RequestAborted);
                return;
            }
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        var processingKey = $"{cacheKey}:processing";
        var processingFlagSet = false;
        if (distributedLock is null)
        {
            var existingFlag = await cache.GetAsync<string>(processingKey, context.RequestAborted);
            if (existingFlag is not null)
            {
                context.Response.Body = originalBody;
                if (await TryReplayCachedResponseAsync(context, cache, cacheKey, originalBody))
                    return;

                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsync("A request with the same idempotency key is already processing.", context.RequestAborted);
                return;
            }

            await cache.SetAsync(processingKey, "1",
                new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
                context.RequestAborted);
            processingFlagSet = true;
        }

        try
        {
            await _next(context);
            buffer.Position = 0;
            var body = buffer.ToArray();

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                await cache.SetAsync(cacheKey, new IdempotencyCacheEntry
                {
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType,
                    Body = body
                }, new CacheEntryOptions { AbsoluteExpirationRelativeToNow = options.Expiration }, context.RequestAborted);
            }

            await originalBody.WriteAsync(body, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
            if (processingFlagSet)
                await cache.RemoveAsync(processingKey, context.RequestAborted);

            if (lockToken is not null && distributedLock is not null)
                await distributedLock.ReleaseLockAsync(lockToken, context.RequestAborted);
        }
    }

    private static async Task<bool> TryReplayCachedResponseAsync(
        HttpContext context,
        ICacheService cache,
        string cacheKey,
        Stream? originalBody)
    {
        await Task.Delay(500, context.RequestAborted);
        var retryResult = await cache.GetAsync<IdempotencyCacheEntry>(cacheKey, context.RequestAborted);
        if (retryResult is null)
            return false;

        context.Response.StatusCode = retryResult.StatusCode;
        context.Response.ContentType = retryResult.ContentType;
        await (originalBody ?? context.Response.Body).WriteAsync(retryResult.Body, context.RequestAborted);
        return true;
    }
}
