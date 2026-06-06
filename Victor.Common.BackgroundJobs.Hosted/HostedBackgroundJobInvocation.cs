using Microsoft.Extensions.DependencyInjection;
using Victor.Common.BackgroundJobs.Abstractions;

namespace Victor.Common.BackgroundJobs.Hosted;

public interface IHostedBackgroundJobInvocation
{
    string Id { get; }

    Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class HostedBackgroundJobInvocation<TJob, TPayload> : IHostedBackgroundJobInvocation
    where TJob : IBackgroundJob<TPayload>
{
    private readonly TPayload _payload;

    public HostedBackgroundJobInvocation(string id, TPayload payload)
    {
        Id = id;
        _payload = payload;
    }

    public string Id { get; }

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TJob>();
        await job.ExecuteAsync(_payload, cancellationToken).ConfigureAwait(false);
    }
}
