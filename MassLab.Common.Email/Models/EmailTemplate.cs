namespace MassLab.Common.Email.Models;

public sealed class EmailTemplate
{
    public required string Key { get; init; }
    public required string Subject { get; init; }
    public required string Html { get; init; }
    public string? Text { get; init; }
    public string? Fingerprint { get; init; }
}

public sealed record RenderedEmailTemplate(string Subject, string Html, string? Text, string? Fingerprint);
