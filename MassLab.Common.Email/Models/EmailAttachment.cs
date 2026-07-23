namespace MassLab.Common.Email.Models;

public sealed class EmailAttachment
{
    public required string FileName { get; init; }
    public required Stream Content { get; init; }
    public string? ContentType { get; init; }
    public bool IsInline { get; init; }
    public string? ContentId { get; init; }
}

public sealed record EmailAttachmentMetadata(string FileName, string? ContentType, bool IsInline, string? ContentId);
