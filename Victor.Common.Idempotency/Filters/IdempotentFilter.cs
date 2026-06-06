using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Victor.Common.Caching.Abstractions;
using Victor.Common.Caching.Models;
using Victor.Common.Idempotency.Configuration;
using Victor.Common.Idempotency.Models;

namespace Victor.Common.Idempotency.Filters;

/// <summary>MVC action filter that deduplicates responses by Idempotency-Key.</summary>
public sealed class IdempotentFilter : IAsyncActionFilter
{
    private readonly ICacheService _cache;
    private readonly IOptions<IdempotencyOptions> _options;
    private readonly IDistributedLock? _distributedLock;

    /// <summary>Creates the filter.</summary>
    public IdempotentFilter(
        ICacheService cache,
        IOptions<IdempotencyOptions> options,
        IDistributedLock? distributedLock = null)
    {
        _cache = cache;
        _options = options;
        _distributedLock = distributedLock;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var options = _options.Value;
        var key = context.HttpContext.Request.Headers[options.HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            context.Result = new BadRequestObjectResult($"{options.HeaderName} header is required.");
            return;
        }

        var cacheKey = BuildCacheKey(options, context.HttpContext.Request, key);
        var cached = await _cache.GetAsync<IdempotencyCacheEntry>(cacheKey, context.HttpContext.RequestAborted);
        if (cached is not null)
        {
            context.Result = ToContentResult(cached);
            return;
        }

        LockToken? lockToken = null;
        if (_distributedLock is not null)
        {
            lockToken = await _distributedLock.AcquireLockAsync(
                cacheKey,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(30),
                context.HttpContext.RequestAborted);

            if (lockToken is null)
            {
                var retryResult = await _cache.GetAsync<IdempotencyCacheEntry>(cacheKey, context.HttpContext.RequestAborted);
                context.Result = retryResult is not null
                    ? ToContentResult(retryResult)
                    : new ConflictObjectResult("A request with the same idempotency key is already processing.");
                return;
            }
        }

        try
        {
            var executed = await next();
            if (executed.Result is ObjectResult objectResult && IsSuccessful(objectResult.StatusCode))
            {
                var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(objectResult.Value);
                await _cache.SetAsync(cacheKey, new IdempotencyCacheEntry
                {
                    StatusCode = objectResult.StatusCode ?? StatusCodes.Status200OK,
                    ContentType = "application/json",
                    Body = body
                }, new CacheEntryOptions { AbsoluteExpirationRelativeToNow = options.Expiration }, context.HttpContext.RequestAborted);
            }
        }
        finally
        {
            if (lockToken is not null && _distributedLock is not null)
                await _distributedLock.ReleaseLockAsync(lockToken, context.HttpContext.RequestAborted);
        }
    }

    private static string BuildCacheKey(IdempotencyOptions options, HttpRequest request, string key)
        => $"{options.CacheKeyPrefix}:{request.Method}:{request.Path}:{key}";

    private static ContentResult ToContentResult(IdempotencyCacheEntry cached) => new()
    {
        StatusCode = cached.StatusCode,
        Content = System.Text.Encoding.UTF8.GetString(cached.Body),
        ContentType = cached.ContentType
    };

    private static bool IsSuccessful(int? statusCode)
        => statusCode is null or >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices;
}
