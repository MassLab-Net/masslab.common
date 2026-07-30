using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Smtp.Configuration;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Smtp.Services;

public sealed class SmtpEmailProviderSenderFactory(IEmailTemplateStore? store = null, IEmailTemplateRenderer? renderer = null) : IEmailProviderSenderFactory
{
    public EmailProviderKind Provider => EmailProviderKind.Smtp;
    public IConfiguredEmailSender Create(EmailProviderConfiguration configuration)
    {
        var value = configuration as SmtpEmailProviderConfiguration ?? throw new ArgumentException("An SMTP configuration is required.", nameof(configuration));
        if (string.IsNullOrWhiteSpace(value.Host) || value.Port is < 1 or > 65535 || string.IsNullOrWhiteSpace(value.DefaultFrom) || string.IsNullOrWhiteSpace(value.UserName) != string.IsNullOrWhiteSpace(value.Password)) throw new ArgumentException("SMTP configuration is incomplete.", nameof(configuration));
        return new Configured(new SmtpEmailSender(Options.Create(new SmtpEmailOptions { Host = value.Host, Port = value.Port, UseSsl = value.UseSsl, UserName = value.UserName, Password = value.Password, DefaultFrom = value.DefaultFrom }), store, renderer));
    }
    private sealed class Configured(SmtpEmailSender inner) : IConfiguredEmailSender { public Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default) => inner.SendAsync(request, cancellationToken); public Task<EmailBatchSendResult> SendBatchAsync(IReadOnlyList<EmailSendRequest> requests, CancellationToken cancellationToken = default) => inner.SendBatchAsync(requests, cancellationToken); public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
}
