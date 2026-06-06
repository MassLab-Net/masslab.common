using Microsoft.Extensions.Diagnostics.HealthChecks;
using MassLab.Common.Caching.Abstractions;

namespace MassLab.Common.Caching.Memory.HealthChecks;

/// <summary>
/// Health check for in-memory cache operations.
/// </summary>
public class MemoryCacheHealthCheck : IHealthCheck
{
    private readonly ICacheService _cacheService;
    private const string TestKey = "__health_check__";

    public MemoryCacheHealthCheck(ICacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testValue = Guid.NewGuid().ToString();
            
            // Test write
            await _cacheService.SetAsync(TestKey, testValue, cancellationToken: cancellationToken);
            
            // Test read
            var retrieved = await _cacheService.GetAsync<string>(TestKey, cancellationToken);
            
            // Test cleanup
            await _cacheService.RemoveAsync(TestKey, cancellationToken);

            if (retrieved == testValue)
            {
                return HealthCheckResult.Healthy("Memory cache is operational.");
            }

            return HealthCheckResult.Unhealthy("Memory cache read/write test failed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Memory cache health check failed.", ex);
        }
    }
}
