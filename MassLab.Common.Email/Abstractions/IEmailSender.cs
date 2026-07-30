using MassLab.Common.Email.Models;

namespace MassLab.Common.Email.Abstractions;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default);
    Task<EmailBatchSendResult> SendBatchAsync(IReadOnlyList<EmailSendRequest> requests, CancellationToken cancellationToken = default);
}
