using HandlebarsDotNet;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;

namespace MassLab.Common.Email.Templates.FileSystem.Services;

public sealed class HandlebarsEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly IHandlebars _handlebars = Handlebars.Create(new HandlebarsConfiguration
    {
        ThrowOnUnresolvedBindingExpression = true
    });

    public Task<RenderedEmailTemplate> RenderAsync(EmailTemplate template, object? data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = data ?? new { };
        return Task.FromResult(new RenderedEmailTemplate(
            _handlebars.Compile(template.Subject)(context),
            _handlebars.Compile(template.Html)(context),
            template.Text is null ? null : _handlebars.Compile(template.Text)(context),
            template.Fingerprint));
    }
}
