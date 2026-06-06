namespace Victor.Common.BackgroundJobs.Configuration;

/// <summary>
/// Options for the background-job subsystem.
/// </summary>
public class BackgroundJobsOptions
{
    /// <summary>Configuration section name (<c>BackgroundJobs</c>).</summary>
    public const string SectionName = "BackgroundJobs";

    /// <summary>
    /// When <c>true</c>, the host runs the worker (consumer). Set to <c>false</c>
    /// in API instances to keep them as schedulers only.
    /// </summary>
    public bool RunWorker { get; set; } = true;

    /// <summary>Worker concurrency / parallelism (default 4).</summary>
    public int WorkerCount { get; set; } = 4;

    /// <summary>
    /// Common queue name used by both Hangfire ("default") and Quartz
    /// (used as group name).
    /// </summary>
    public string QueueName { get; set; } = "default";
}
