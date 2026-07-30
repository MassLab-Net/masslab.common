namespace MassLab.Common.Email.Models;

public abstract record EmailContent;

public sealed record RawEmailContent(string Subject, string? Html, string? Text = null) : EmailContent;

public sealed record LocalTemplateEmailContent(string TemplateKey, object? Data = null) : EmailContent;

public sealed record ProviderTemplateEmailContent(string TemplateIdOrAlias, IReadOnlyDictionary<string, object?>? Variables = null) : EmailContent;
