namespace MassLab.Common.Email.Models;

public enum EmailDeliveryStatus { Accepted, Sent, Delivered, Delayed, Bounced, Failed, Suppressed, Complained }
public enum EmailEngagementType { Opened, Clicked }

public sealed class EmailLifecycleEvent
{
    public required string EventId { get; init; }
    public required string Provider { get; init; }
    public required string ProviderEventType { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public EmailDeliveryStatus? DeliveryStatus { get; init; }
    public EmailEngagementType? EngagementType { get; init; }
    public EmailAddress? Recipient { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
