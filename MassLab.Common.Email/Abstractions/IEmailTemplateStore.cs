using MassLab.Common.Email.Models;

namespace MassLab.Common.Email.Abstractions;

public interface IEmailTemplateStore
{
    Task<EmailTemplate?> GetAsync(string templateKey, CancellationToken cancellationToken = default);
}
