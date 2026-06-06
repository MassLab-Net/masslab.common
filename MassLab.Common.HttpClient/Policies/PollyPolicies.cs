using Polly;
using Polly.Bulkhead;
using Polly.Extensions.Http;

namespace MassLab.Common.HttpClient.Policies;

/// <summary>
/// Provides Polly resilience policies for HTTP clients including retry and circuit breaker patterns.
/// </summary>
public static class PollyPolicies
{
    /// <summary>
    /// Gets a retry policy that handles transient HTTP errors with exponential backoff and jitter.
    /// </summary>
    /// <param name="retryCount">The number of retry attempts. Default is 3.</param>
    /// <returns>An async policy for handling HTTP response messages.</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount = 3)
    {
        if (retryCount < 0)
            throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "Retry count cannot be negative.");

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)));
    }

    /// <summary>
    /// Gets a circuit breaker policy that opens after configured failures for a configured duration.
    /// </summary>
    /// <param name="exceptionsBeforeBreaking">The number of consecutive failures before opening the circuit. Default is 5.</param>
    /// <param name="durationOfBreakInSeconds">The duration in seconds to keep the circuit open. Default is 30.</param>
    /// <returns>An async policy for handling HTTP response messages.</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        int exceptionsBeforeBreaking = 5,
        int durationOfBreakInSeconds = 30)
    {
        if (exceptionsBeforeBreaking <= 0)
            throw new ArgumentOutOfRangeException(nameof(exceptionsBeforeBreaking), exceptionsBeforeBreaking, "Exceptions before breaking must be greater than zero.");
        if (durationOfBreakInSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationOfBreakInSeconds), durationOfBreakInSeconds, "Break duration must be greater than zero.");

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(exceptionsBeforeBreaking, TimeSpan.FromSeconds(durationOfBreakInSeconds));
    }

    /// <summary>
    /// Gets a bulkhead (concurrency limiter) policy.
    /// </summary>
    /// <param name="maxParallelization">Maximum concurrent executions. Default is 10.</param>
    /// <param name="maxQueuingActions">Maximum queued actions. Default is 5.</param>
    /// <returns>An async bulkhead policy for HTTP response messages.</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetBulkheadPolicy(
        int maxParallelization = 10,
        int maxQueuingActions = 5)
    {
        if (maxParallelization <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelization), maxParallelization, "Max parallelization must be greater than zero.");
        if (maxQueuingActions < 0)
            throw new ArgumentOutOfRangeException(nameof(maxQueuingActions), maxQueuingActions, "Max queued actions cannot be negative.");

        return Policy.BulkheadAsync<HttpResponseMessage>(maxParallelization, maxQueuingActions);
    }
}
