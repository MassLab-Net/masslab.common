namespace Victor.Common.Outbox.Configuration;

/// <summary>
/// Outbox dispatcher options.
/// </summary>
public class OutboxOptions
{
    /// <summary>Configuration section name (<c>Outbox</c>).</summary>
    public const string SectionName = "Outbox";

    /// <summary>Polling interval (default 5 seconds).</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Max messages dispatched per polling cycle (default 100).</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Maximum number of dispatch attempts per message (default 10).</summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>Whether to delete successfully-dispatched messages immediately (default <c>false</c>).</summary>
    public bool DeleteAfterDispatch { get; set; } = false;

    /// <summary>Number of days to retain processed messages before cleanup (default 7). Set to 0 to disable cleanup.</summary>
    public int RetentionDays { get; set; } = 7;
}
