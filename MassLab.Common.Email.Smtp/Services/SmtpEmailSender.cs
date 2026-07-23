using System.Net;
using System.Net.Mail;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Services;
using MassLab.Common.Email.Smtp.Configuration;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Smtp.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly EmailContentResolver _resolver;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options, IEmailTemplateStore? store = null, IEmailTemplateRenderer? renderer = null)
    {
        _options = options.Value;
        _resolver = new EmailContentResolver(store, renderer);
    }

    public async Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.To.Count == 0) return Rejected(request, "validation_error", "At least one recipient is required.");
            var content = await _resolver.ResolveAsync(request.Content, cancellationToken);
            if (content.Subject is null) return Rejected(request, "unsupported_content", "SMTP does not support provider-hosted templates.");
            var from = request.From?.ToString() ?? _options.DefaultFrom;
            if (string.IsNullOrWhiteSpace(from)) return Rejected(request, "validation_error", "A sender address is required.");
            using var message = BuildMessage(request, content, from);
            using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.UseSsl };
            if (!string.IsNullOrWhiteSpace(_options.UserName)) client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            await client.SendMailAsync(message, cancellationToken);
            var messageId = message.Headers["Message-ID"] ?? message.Headers["Message-Id"];
            return new EmailSendResult
            {
                Status = EmailSubmissionStatus.Accepted, Provider = "SMTP", ProviderMessageId = messageId, CorrelationId = request.CorrelationId,
                Message = request.IncludeRenderedContent ? Snapshot(request, from, content) : null
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException)
        {
            return Rejected(request, "rendering_error", ex.Message);
        }
        catch (Exception ex)
        {
            return Rejected(request, "smtp_error", ex.Message);
        }
    }

    public async Task<EmailBatchSendResult> SendBatchAsync(IReadOnlyList<EmailSendRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = new List<EmailSendResult>(requests.Count);
        foreach (var request in requests) results.Add(await SendAsync(request, cancellationToken));
        return new EmailBatchSendResult { Results = results };
    }

    private static MailMessage BuildMessage(EmailSendRequest request, ResolvedEmailContent content, string from)
    {
        var message = new MailMessage { Subject = content.Subject!, From = new MailAddress(from) };
        AddAddresses(message.To, request.To); AddAddresses(message.CC, request.Cc); AddAddresses(message.Bcc, request.Bcc); AddAddresses(message.ReplyToList, request.ReplyTo);
        foreach (var header in request.Headers) message.Headers.Add(header.Key, header.Value);
        foreach (var tag in request.Tags) message.Headers[$"X-MassLab-Tag-{tag.Key}"] = tag.Value;
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey)) message.Headers["X-MassLab-Idempotency-Key"] = request.IdempotencyKey;
        message.IsBodyHtml = !string.IsNullOrWhiteSpace(content.Html);
        message.Body = content.Html ?? content.Text ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(content.Html) && !string.IsNullOrWhiteSpace(content.Text)) message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(content.Text, null, "text/plain"));
        foreach (var attachment in request.Attachments)
        {
            var item = new Attachment(attachment.Content, attachment.FileName, attachment.ContentType ?? "application/octet-stream") { ContentId = attachment.ContentId };
            if (attachment.IsInline) item.ContentDisposition!.Inline = true;
            message.Attachments.Add(item);
        }
        return message;
    }

    private static void AddAddresses(MailAddressCollection destination, IEnumerable<EmailAddress> addresses)
    {
        foreach (var address in addresses) destination.Add(new MailAddress(address.Address, address.DisplayName));
    }
    private static EmailSendResult Rejected(EmailSendRequest request, string code, string message) => new() { Status = EmailSubmissionStatus.Rejected, Provider = "SMTP", CorrelationId = request.CorrelationId, ErrorCode = code, ErrorMessage = message };
    private static EmailMessageSnapshot Snapshot(EmailSendRequest request, string from, ResolvedEmailContent content) { var sender = new MailAddress(from); return new EmailMessageSnapshot { From = new EmailAddress(sender.Address, sender.DisplayName), To = request.To, Cc = request.Cc, Bcc = request.Bcc, ReplyTo = request.ReplyTo, Subject = content.Subject!, Html = content.Html, Text = content.Text, TemplateIdentifier = content.TemplateIdentifier, TemplateFingerprint = content.TemplateFingerprint, Attachments = request.Attachments.Select(x => new EmailAttachmentMetadata(x.FileName, x.ContentType, x.IsInline, x.ContentId)).ToArray() }; }
}
