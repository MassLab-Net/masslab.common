using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Victor.Common.Caching.Abstractions;
using Victor.Common.Caching.Memory;
using Victor.Common.Caching.Memory.Configuration;
using Victor.Common.Caching.Models;
using Victor.Common.Idempotency.Configuration;
using Victor.Common.Idempotency.Middleware;

namespace Victor.Common.Idempotency.Tests;

public class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task Middleware_replays_cached_successful_response_for_same_key()
    {
        var calls = 0;
        var cache = new MemoryCacheService(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            Options.Create(new VictorMemoryCacheOptions()));
        var middleware = new IdempotencyMiddleware(async context =>
        {
            calls++;
            await context.Response.WriteAsync($"call-{calls}");
        });

        var first = NewContext();
        await middleware.InvokeAsync(first, cache, Options.Create(new IdempotencyOptions()));

        var second = NewContext();
        await middleware.InvokeAsync(second, cache, Options.Create(new IdempotencyOptions()));

        calls.Should().Be(1);
        ReadBody(second).Should().Be("call-1");
    }

    [Fact]
    public async Task Middleware_returns_conflict_when_same_key_is_already_locked()
    {
        var calls = 0;
        var cache = new MemoryCacheService(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            Options.Create(new VictorMemoryCacheOptions()));
        var middleware = new IdempotencyMiddleware(context =>
        {
            calls++;
            return context.Response.WriteAsync("should-not-run");
        });
        var context = NewContext();
        var lockService = new RejectingDistributedLock();

        await middleware.InvokeAsync(context, cache, Options.Create(new IdempotencyOptions()), lockService);

        calls.Should().Be(0);
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        ReadBody(context).Should().Contain("already processing");
    }

    private static DefaultHttpContext NewContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/orders";
        context.Request.Headers["Idempotency-Key"] = "same-key";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return new StreamReader(context.Response.Body).ReadToEnd();
    }

    private sealed class RejectingDistributedLock : IDistributedLock
    {
        public Task<LockToken?> AcquireLockAsync(
            string key,
            TimeSpan timeout,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
            => Task.FromResult<LockToken?>(null);

        public Task<bool> ReleaseLockAsync(LockToken token, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
