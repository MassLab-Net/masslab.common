using MassLab.Common.Email.Models;

namespace MassLab.Common.Email.Abstractions;

public interface IConfiguredEmailSender : IEmailSender, IAsyncDisposable { }

public interface IEmailSenderFactory
{
    IConfiguredEmailSender Create(EmailProviderConfiguration configuration);
}

public interface IEmailProviderSenderFactory
{
    EmailProviderKind Provider { get; }
    IConfiguredEmailSender Create(EmailProviderConfiguration configuration);
}
