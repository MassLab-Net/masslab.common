using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;

namespace MassLab.Common.Email.Services;

public sealed class EmailContentResolver(IEmailTemplateStore? store = null, IEmailTemplateRenderer? renderer = null)
{
    public async Task<ResolvedEmailContent> ResolveAsync(EmailContent content, CancellationToken cancellationToken)
    {
        switch (content)
        {
            case RawEmailContent raw when !string.IsNullOrWhiteSpace(raw.Subject) && (!string.IsNullOrWhiteSpace(raw.Html) || !string.IsNullOrWhiteSpace(raw.Text)):
                return new ResolvedEmailContent(raw.Subject, raw.Html, raw.Text, null, null, null);
            case LocalTemplateEmailContent local:
                if (store is null || renderer is null) throw new InvalidOperationException("Local email templates are not registered.");
                var template = await store.GetAsync(local.TemplateKey, cancellationToken) ?? throw new InvalidOperationException($"Email template '{local.TemplateKey}' was not found.");
                var rendered = await renderer.RenderAsync(template, local.Data, cancellationToken);
                return new ResolvedEmailContent(rendered.Subject, rendered.Html, rendered.Text, local.TemplateKey, rendered.Fingerprint, null);
            case ProviderTemplateEmailContent provider:
                return new ResolvedEmailContent(null, null, null, provider.TemplateIdOrAlias, null, provider.Variables);
            default:
                throw new ArgumentException("Email content must have a subject and an HTML or text body.", nameof(content));
        }
    }
}

public sealed record ResolvedEmailContent(string? Subject, string? Html, string? Text, string? TemplateIdentifier, string? TemplateFingerprint, IReadOnlyDictionary<string, object?>? ProviderVariables);
