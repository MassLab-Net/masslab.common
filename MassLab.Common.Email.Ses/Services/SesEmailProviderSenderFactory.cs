using Amazon;
using Amazon.SimpleEmailV2;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Ses.Configuration;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Ses.Services;

public sealed class SesEmailProviderSenderFactory : IEmailProviderSenderFactory
{
    public EmailProviderKind Provider => EmailProviderKind.Ses;
    public IConfiguredEmailSender Create(EmailProviderConfiguration configuration)
    {
        var value = configuration as SesEmailProviderConfiguration ?? throw new ArgumentException("An SES configuration is required.", nameof(configuration));
        if (string.IsNullOrWhiteSpace(value.Region) || string.IsNullOrWhiteSpace(value.DefaultFrom) || string.IsNullOrWhiteSpace(value.AccessKey) != string.IsNullOrWhiteSpace(value.SecretKey)) throw new ArgumentException("SES configuration is incomplete.", nameof(configuration));
        var clientConfig = new AmazonSimpleEmailServiceV2Config { RegionEndpoint = RegionEndpoint.GetBySystemName(value.Region) };
        IAmazonSimpleEmailServiceV2 client = string.IsNullOrWhiteSpace(value.AccessKey) ? new AmazonSimpleEmailServiceV2Client(clientConfig) : new AmazonSimpleEmailServiceV2Client(value.AccessKey, value.SecretKey, clientConfig);
        return new Configured(client, new SesEmailSender(client, Options.Create(new SesEmailOptions { Region = value.Region, AccessKey = value.AccessKey, SecretKey = value.SecretKey, DefaultFrom = value.DefaultFrom, ConfigurationSetName = value.ConfigurationSetName })));
    }
    private sealed class Configured(IAmazonSimpleEmailServiceV2 client, SesEmailSender inner) : IConfiguredEmailSender { public Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default) => inner.SendAsync(request, cancellationToken); public Task<EmailBatchSendResult> SendBatchAsync(IReadOnlyList<EmailSendRequest> requests, CancellationToken cancellationToken = default) => inner.SendBatchAsync(requests, cancellationToken); public ValueTask DisposeAsync() { client.Dispose(); return ValueTask.CompletedTask; } }
}
