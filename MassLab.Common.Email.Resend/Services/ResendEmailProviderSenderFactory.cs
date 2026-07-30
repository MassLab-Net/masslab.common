using System.Net.Http.Headers;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Resend.Configuration;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Resend.Services;

public sealed class ResendEmailProviderSenderFactory(IHttpClientFactory clients) : IEmailProviderSenderFactory
{
    public EmailProviderKind Provider => EmailProviderKind.Resend;
    public IConfiguredEmailSender Create(EmailProviderConfiguration configuration)
    {
        var value = configuration as ResendEmailProviderConfiguration
            ?? throw new ArgumentException("A Resend configuration is required.", nameof(configuration));
        if (string.IsNullOrWhiteSpace(value.ApiKey) || string.IsNullOrWhiteSpace(value.DefaultFrom) || !Uri.TryCreate(value.BaseUrl, UriKind.Absolute, out var baseUri))
            throw new ArgumentException("Resend configuration is incomplete.", nameof(configuration));
        var client = clients.CreateClient("MassLab.Common.Email.Resend.Dynamic");
        client.BaseAddress = new Uri(baseUri.ToString().TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value.ApiKey);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MassLab.Common.Email/1.0");
        return new Configured(new ResendEmailSender(client, Options.Create(new ResendEmailOptions { ApiKey = value.ApiKey, DefaultFrom = value.DefaultFrom, BaseUrl = value.BaseUrl })));
    }
    private sealed class Configured(ResendEmailSender inner) : IConfiguredEmailSender
    {
        public Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default) => inner.SendAsync(request, cancellationToken);
        public Task<EmailBatchSendResult> SendBatchAsync(IReadOnlyList<EmailSendRequest> requests, CancellationToken cancellationToken = default) => inner.SendBatchAsync(requests, cancellationToken);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
