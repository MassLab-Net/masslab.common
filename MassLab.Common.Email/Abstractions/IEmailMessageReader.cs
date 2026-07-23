using MassLab.Common.Email.Models;

namespace MassLab.Common.Email.Abstractions;

/// <summary>Optional provider capability for retrieving a submitted email.</summary>
public interface IEmailMessageReader
{
    Task<EmailMessageSnapshot?> GetAsync(string providerMessageId, CancellationToken cancellationToken = default);
}
