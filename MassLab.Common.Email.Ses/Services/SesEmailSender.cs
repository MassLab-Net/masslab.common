using System.Text.Json;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Services;
using MassLab.Common.Email.Ses.Configuration;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Ses.Services;

public sealed class SesEmailSender : IEmailSender
{
    private readonly IAmazonSimpleEmailServiceV2 _client;
    private readonly SesEmailOptions _options;
    private readonly EmailContentResolver _resolver;

    public SesEmailSender(IAmazonSimpleEmailServiceV2 client, IOptions<SesEmailOptions> options, IEmailTemplateStore? store = null, IEmailTemplateRenderer? renderer = null)
    {
        _client = client;
        _options = options.Value;
        _resolver = new EmailContentResolver(store, renderer);
    }

    public async Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.To.Count == 0) return Rejected(request, "validation_error", "At least one recipient is required.");
            var content = await _resolver.ResolveAsync(request.Content, cancellationToken);
            var from = request.From?.ToString() ?? _options.DefaultFrom;
            if (string.IsNullOrWhiteSpace(from)) return Rejected(request, "validation_error", "A sender address is required.");
            var response = await _client.SendEmailAsync(CreateRequest(request, content, from), cancellationToken);
            return new EmailSendResult
            {
                Status = EmailSubmissionStatus.Accepted, Provider = "SES", ProviderMessageId = response.MessageId,
                CorrelationId = request.CorrelationId,
                Message = request.IncludeRenderedContent && content.Subject is not null ? Snapshot(request, from, content) : null,
                ProviderMetadata = string.IsNullOrWhiteSpace(_options.ConfigurationSetName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["configurationSet"] = _options.ConfigurationSetName }
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Rejected(request, "rendering_error", ex.Message);
        }
        catch (AmazonServiceException ex)
        {
            return Rejected(request, ex.ErrorCode ?? "ses_error", ex.Message);
        }
    }

    public async Task<EmailBatchSendResult> SendBatchAsync(IReadOnlyList<EmailSendRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = new List<EmailSendResult>(requests.Count);
        foreach (var request in requests) results.Add(await SendAsync(request, cancellationToken));
        return new EmailBatchSendResult { Results = results };
    }

    private SendEmailRequest CreateRequest(EmailSendRequest request, ResolvedEmailContent content, string from)
    {
        var send = new SendEmailRequest
        {
            FromEmailAddress = from,
            Destination = new Destination { ToAddresses = request.To.Select(x => x.Address).ToList(), CcAddresses = request.Cc.Select(x => x.Address).ToList(), BccAddresses = request.Bcc.Select(x => x.Address).ToList() },
            ReplyToAddresses = request.ReplyTo.Select(x => x.Address).ToList(),
            ConfigurationSetName = _options.ConfigurationSetName,
            EmailTags = request.Tags.Select(x => new MessageTag { Name = x.Key, Value = x.Value }).ToList()
        };
        if (!string.IsNullOrWhiteSpace(request.CorrelationId)) send.EmailTags.Add(new MessageTag { Name = "correlation_id", Value = request.CorrelationId });
        if (content.Subject is null)
        {
            if (request.Attachments.Count > 0 || request.Headers.Count > 0) throw new InvalidOperationException("SES hosted templates cannot be combined with attachments or custom headers.");
            send.Content = new Amazon.SimpleEmailV2.Model.EmailContent { Template = new Template { TemplateName = content.TemplateIdentifier, TemplateData = JsonSerializer.Serialize(content.ProviderVariables ?? new Dictionary<string, object?>()) } };
            return send;
        }

        var message = new Message
        {
            Subject = new Content { Data = content.Subject },
            Body = new Body { Html = string.IsNullOrWhiteSpace(content.Html) ? null : new Content { Data = content.Html }, Text = string.IsNullOrWhiteSpace(content.Text) ? null : new Content { Data = content.Text } },
            Headers = request.Headers.Select(x => new MessageHeader { Name = x.Key, Value = x.Value }).ToList()
        };
        foreach (var attachment in request.Attachments)
        {
            var buffer = new MemoryStream();
            attachment.Content.CopyTo(buffer);
            message.Attachments.Add(new Attachment { FileName = attachment.FileName, ContentType = attachment.ContentType, ContentId = attachment.ContentId, RawContent = new MemoryStream(buffer.ToArray()) });
        }
        send.Content = new Amazon.SimpleEmailV2.Model.EmailContent { Simple = message };
        return send;
    }

    private static EmailSendResult Rejected(EmailSendRequest request, string code, string message) => new() { Status = EmailSubmissionStatus.Rejected, Provider = "SES", CorrelationId = request.CorrelationId, ErrorCode = code, ErrorMessage = message };
    private static EmailMessageSnapshot Snapshot(EmailSendRequest request, string from, ResolvedEmailContent content) => new() { From = ParseAddress(from), To = request.To, Cc = request.Cc, Bcc = request.Bcc, ReplyTo = request.ReplyTo, Subject = content.Subject!, Html = content.Html, Text = content.Text, TemplateIdentifier = content.TemplateIdentifier, TemplateFingerprint = content.TemplateFingerprint, Attachments = request.Attachments.Select(x => new EmailAttachmentMetadata(x.FileName, x.ContentType, x.IsInline, x.ContentId)).ToArray() };
    private static EmailAddress ParseAddress(string value) { var start = value.LastIndexOf('<'); var end = value.LastIndexOf('>'); return start >= 0 && end > start ? new EmailAddress(value[(start + 1)..end].Trim(), value[..start].Trim()) : new EmailAddress(value); }
}
