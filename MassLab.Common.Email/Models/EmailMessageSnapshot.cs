namespace MassLab.Common.Email.Models;

public sealed class EmailMessageSnapshot
{
    public required EmailAddress From { get; init; }
    public required IReadOnlyList<EmailAddress> To { get; init; }
    public IReadOnlyList<EmailAddress> Cc { get; init; } = [];
    public IReadOnlyList<EmailAddress> Bcc { get; init; } = [];
    public IReadOnlyList<EmailAddress> ReplyTo { get; init; } = [];
    public required string Subject { get; init; }
    public string? Html { get; init; }
    public string? Text { get; init; }
    public string? TemplateIdentifier { get; init; }
    public string? TemplateFingerprint { get; init; }
    public bool IsProviderConfirmedContent { get; init; }
    public IReadOnlyList<EmailAttachmentMetadata> Attachments { get; init; } = [];
}
