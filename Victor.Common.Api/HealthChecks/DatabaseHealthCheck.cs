using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Victor.Common.Api.HealthChecks;

/// <summary>
/// Health check for database connectivity using DbContext with timeout support.
/// </summary>
public class DbContextHealthCheck<TContext> : IHealthCheck where TContext : DbContext
{
    private readonly TContext _context;
    private readonly TimeSpan _timeout;

    public DbContextHealthCheck(TContext context, TimeSpan timeout)
    {
        _context = context;
        _timeout = timeout;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(_timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await _context.Database.CanConnectAsync(linkedCts.Token);

            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database health check was cancelled");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy($"Database health check timed out after {_timeout.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed",
                ex);
        }
    }
}

