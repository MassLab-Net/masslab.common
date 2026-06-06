using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Victor.Common.Caching.Redis.Configuration;

namespace Victor.Common.Caching.Redis.HealthChecks;

/// <summary>
/// Health check for Redis cache connectivity and availability.
/// </summary>
public class RedisCacheHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisCacheOptions _options;

    public RedisCacheHealthCheck(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisCacheOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _connectionMultiplexer.GetEndPoints().FirstOrDefault();
            var endpointString = endpoint?.ToString() ?? "unknown";

            var stopwatch = Stopwatch.StartNew();
            var server = _connectionMultiplexer.GetServer(endpoint!);
            
            // Ping Redis with 1 second timeout
            var pingTask = server.PingAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            var completedTask = await Task.WhenAny(pingTask, timeoutTask);

            if (completedTask == pingTask)
            {
                stopwatch.Stop();
                var latency = await pingTask;

                var data = new Dictionary<string, object>
                {
                    { "endpoint", endpointString },
                    { "latency_ms", latency.TotalMilliseconds },
                    { "instance_name", _options.InstanceName ?? "none" }
                };

                return HealthCheckResult.Healthy("Redis cache is operational.", data);
            }
            else
            {
                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    { "endpoint", endpointString },
                    { "timeout_ms", 1000 },
                    { "instance_name", _options.InstanceName ?? "none" }
                };

                return HealthCheckResult.Unhealthy("Redis ping timeout exceeded 1000ms.", null, data);
            }
        }
        catch (Exception ex)
        {
            var endpoint = _connectionMultiplexer.GetEndPoints().FirstOrDefault();
            var endpointString = endpoint?.ToString() ?? "unknown";

            var data = new Dictionary<string, object>
            {
                { "endpoint", endpointString },
                { "instance_name", _options.InstanceName ?? "none" }
            };

            return HealthCheckResult.Unhealthy("Redis health check failed.", ex, data);
        }
    }
}
