using MassLab.Common.Email.Models;

namespace MassLab.Common.Email.Abstractions;

public interface IEmailTemplateRenderer
{
    Task<RenderedEmailTemplate> RenderAsync(EmailTemplate template, object? data, CancellationToken cancellationToken = default);
}
