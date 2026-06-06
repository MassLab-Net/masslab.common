namespace MassLab.Common.Messaging.Configuration;

/// <summary>
/// Generic messaging options shared by all transports.
/// </summary>
public class MessagingOptions
{
    /// <summary>Configuration section name (<c>Messaging</c>).</summary>
    public const string SectionName = "Messaging";

    /// <summary>Logical service / app name used as default queue/topic prefix.</summary>
    public string ServiceName { get; set; } = "masslab-service";

    /// <summary>Default topic / exchange name (transport-specific).</summary>
    public string Topic { get; set; } = "masslab.events";

    /// <summary>Whether to propagate <c>traceparent</c> as a message header.</summary>
    public bool PropagateTraceContext { get; set; } = true;

    /// <summary>Whether to propagate the tenant id as a message header.</summary>
    public bool PropagateTenantId { get; set; } = true;
}
