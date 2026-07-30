namespace MassLab.Common.Email.Models;

public enum EmailSubmissionStatus { Accepted, Rejected }

public sealed class EmailSendResult
{
    public required EmailSubmissionStatus Status { get; init; }
    public required string Provider { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public EmailMessageSnapshot? Message { get; init; }
    public IReadOnlyDictionary<string, string> ProviderMetadata { get; init; } = new Dictionary<string, string>();
    public bool IsAccepted => Status == EmailSubmissionStatus.Accepted;
}

public sealed class EmailBatchSendResult
{
    public required IReadOnlyList<EmailSendResult> Results { get; init; }
    public bool IsAccepted => Results.All(x => x.IsAccepted);
}
