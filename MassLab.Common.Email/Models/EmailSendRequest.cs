namespace MassLab.Common.Email.Models;

public sealed class EmailSendRequest
{
    public EmailAddress? From { get; init; }
    public required IReadOnlyList<EmailAddress> To { get; init; }
    public IReadOnlyList<EmailAddress> Cc { get; init; } = [];
    public IReadOnlyList<EmailAddress> Bcc { get; init; } = [];
    public IReadOnlyList<EmailAddress> ReplyTo { get; init; } = [];
    public required EmailContent Content { get; init; }
    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public string? CorrelationId { get; init; }
    public string? IdempotencyKey { get; init; }
    public bool IncludeRenderedContent { get; init; }
}
