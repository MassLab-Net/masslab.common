using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Victor.Common.BackgroundJobs.Abstractions;
using Victor.Common.BackgroundJobs.Extensions;
using Victor.Common.BackgroundJobs.Hosted;
using Victor.Common.BackgroundJobs.Hosted.Extensions;

namespace Victor.Common.BackgroundJobs.Tests;

public class HostedBackgroundJobsTests
{
    [Fact]
    public async Task Hosted_scheduler_executes_enqueued_jobs()
    {
        var recorder = new JobRecorder();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(recorder);
        services.AddBackgroundJob<RecordPayloadJob, string>();
        services.AddVictorHostedBackgroundJobs();
        await using var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();

        foreach (var hostedService in hostedServices)
            await hostedService.StartAsync(CancellationToken.None);

        var scheduler = provider.GetRequiredService<IBackgroundJobScheduler>();
        await scheduler.EnqueueAsync<RecordPayloadJob, string>("hello");

        var completed = await recorder.WaitAsync(TimeSpan.FromSeconds(5));

        completed.Should().BeTrue();
        recorder.Payloads.Should().ContainSingle("hello");

        foreach (var hostedService in hostedServices.Reverse())
            await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Hosted_provider_respects_run_worker_false()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundJobs:RunWorker"] = "false"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVictorHostedBackgroundJobs(configuration);
        using var provider = services.BuildServiceProvider();

        provider.GetServices<IHostedService>()
            .Should()
            .NotContain(service => service is HostedBackgroundJobWorker);
    }

    [Fact]
    public async Task Recurring_bootstrapper_runs_at_startup()
    {
        var recorder = new BootstrapperRecorder();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(recorder);
        services.AddRecurringJobBootstrapper<TestBootstrapper>();
        services.AddVictorHostedBackgroundJobs();
        await using var provider = services.BuildServiceProvider();
        var bootstrapper = provider.GetServices<IHostedService>()
            .Single(service => service.GetType().Name == "RecurringJobBootstrapperHostedService");

        await bootstrapper.StartAsync(CancellationToken.None);

        recorder.Registered.Should().BeTrue();
    }

    private sealed class JobRecorder
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly List<string> _payloads = [];

        public IReadOnlyCollection<string> Payloads => _payloads;

        public void Record(string payload)
        {
            lock (_payloads)
                _payloads.Add(payload);

            _completion.TrySetResult();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_completion.Task, Task.Delay(timeout));
            return completed == _completion.Task;
        }
    }

    private sealed class RecordPayloadJob : IBackgroundJob<string>
    {
        private readonly JobRecorder _recorder;

        public RecordPayloadJob(JobRecorder recorder) => _recorder = recorder;

        public Task ExecuteAsync(string payload, CancellationToken cancellationToken = default)
        {
            _recorder.Record(payload);
            return Task.CompletedTask;
        }
    }

    private sealed class BootstrapperRecorder
    {
        public bool Registered { get; set; }
    }

    private sealed class TestBootstrapper : IRecurringJobBootstrapper
    {
        private readonly BootstrapperRecorder _recorder;

        public TestBootstrapper(BootstrapperRecorder recorder) => _recorder = recorder;

        public Task RegisterAsync(IBackgroundJobScheduler scheduler, CancellationToken cancellationToken = default)
        {
            _recorder.Registered = true;
            return Task.CompletedTask;
        }
    }
}
