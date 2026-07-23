using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Resend.Configuration;
using MassLab.Common.Email.Services;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Resend.Services;

public sealed class ResendEmailSender : IEmailSender, IEmailMessageReader
{
    private readonly HttpClient _client;
    private readonly ResendEmailOptions _options;
    private readonly EmailContentResolver _resolver;

    public ResendEmailSender(HttpClient client, IOptions<ResendEmailOptions> options, IEmailTemplateStore? store = null, IEmailTemplateRenderer? renderer = null)
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

            var payload = await ToPayloadAsync(request, content, from, cancellationToken);
            using var message = new HttpRequestMessage(HttpMethod.Post, "emails") { Content = JsonContent.Create(payload) };
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey)) message.Headers.Add("Idempotency-Key", request.IdempotencyKey);
            using var response = await _client.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Rejected(request, $"resend_{(int)response.StatusCode}", await response.Content.ReadAsStringAsync(cancellationToken));

            var sent = await response.Content.ReadFromJsonAsync<SendResponse>(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(sent?.Id)) return Rejected(request, "resend_invalid_response", "Resend did not return an email identifier.");
            EmailMessageSnapshot? snapshot = null;
            if (request.IncludeRenderedContent)
            {
                snapshot = content.Subject is not null ? Snapshot(request, from, content, false) : await GetAsync(sent.Id, cancellationToken);
                snapshot ??= Snapshot(request, from, content, false);
            }
            return new EmailSendResult { Status = EmailSubmissionStatus.Accepted, Provider = "Resend", ProviderMessageId = sent.Id, CorrelationId = request.CorrelationId, Message = snapshot };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Rejected(request, "rendering_error", ex.Message);
        }
    }

    public async Task<EmailBatchSendResult> SendBatchAsync(IReadOnlyList<EmailSendRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = new List<EmailSendResult>(requests.Count);
        foreach (var request in requests) results.Add(await SendAsync(request, cancellationToken));
        return new EmailBatchSendResult { Results = results };
    }

    public async Task<EmailMessageSnapshot?> GetAsync(string providerMessageId, CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync($"emails/{Uri.EscapeDataString(providerMessageId)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var email = await response.Content.ReadFromJsonAsync<SentEmailResponse>(cancellationToken: cancellationToken);
        if (email is null || string.IsNullOrWhiteSpace(email.Subject)) return null;
        return new EmailMessageSnapshot
        {
            From = ParseAddress(email.From), To = email.To?.Select(ParseAddress).ToArray() ?? [],
            Cc = email.Cc?.Select(ParseAddress).ToArray() ?? [], Bcc = email.Bcc?.Select(ParseAddress).ToArray() ?? [],
            ReplyTo = email.ReplyTo?.Select(ParseAddress).ToArray() ?? [], Subject = email.Subject,
            Html = email.Html, Text = email.Text, IsProviderConfirmedContent = true
        };
    }

    private async Task<object> ToPayloadAsync(EmailSendRequest request, ResolvedEmailContent content, string from, CancellationToken cancellationToken)
    {
        var attachments = new List<object>();
        foreach (var item in request.Attachments)
        {
            using var buffer = new MemoryStream();
            await item.Content.CopyToAsync(buffer, cancellationToken);
            attachments.Add(new { filename = item.FileName, content = Convert.ToBase64String(buffer.ToArray()), content_type = item.ContentType, content_disposition = item.IsInline ? "inline" : "attachment", content_id = item.ContentId });
        }
        var tags = request.Tags.Select(x => new { name = x.Key, value = x.Value }).ToArray();
        var basePayload = new Dictionary<string, object?> { ["from"] = from, ["to"] = request.To.Select(x => x.ToString()).ToArray(), ["cc"] = request.Cc.Select(x => x.ToString()).ToArray(), ["bcc"] = request.Bcc.Select(x => x.ToString()).ToArray(), ["reply_to"] = request.ReplyTo.Select(x => x.ToString()).ToArray(), ["headers"] = request.Headers, ["tags"] = tags, ["attachments"] = attachments };
        if (content.Subject is null) basePayload["template"] = new { id = content.TemplateIdentifier, variables = content.ProviderVariables ?? new Dictionary<string, object?>() };
        else { basePayload["subject"] = content.Subject; basePayload["html"] = content.Html; basePayload["text"] = content.Text; }
        return basePayload;
    }

    private static EmailSendResult Rejected(EmailSendRequest request, string code, string message) => new() { Status = EmailSubmissionStatus.Rejected, Provider = "Resend", CorrelationId = request.CorrelationId, ErrorCode = code, ErrorMessage = message };
    private static EmailMessageSnapshot Snapshot(EmailSendRequest request, string from, ResolvedEmailContent content, bool confirmed) => new() { From = ParseAddress(from), To = request.To, Cc = request.Cc, Bcc = request.Bcc, ReplyTo = request.ReplyTo, Subject = content.Subject ?? string.Empty, Html = content.Html, Text = content.Text, TemplateIdentifier = content.TemplateIdentifier, TemplateFingerprint = content.TemplateFingerprint, IsProviderConfirmedContent = confirmed, Attachments = request.Attachments.Select(x => new EmailAttachmentMetadata(x.FileName, x.ContentType, x.IsInline, x.ContentId)).ToArray() };
    private static EmailAddress ParseAddress(string value)
    {
        var start = value.LastIndexOf('<'); var end = value.LastIndexOf('>');
        return start >= 0 && end > start ? new EmailAddress(value[(start + 1)..end].Trim(), value[..start].Trim()) : new EmailAddress(value);
    }
    private sealed class SendResponse { public string? Id { get; init; } }
    private sealed class SentEmailResponse { public string From { get; init; } = string.Empty; public string[]? To { get; init; } public string[]? Cc { get; init; } public string[]? Bcc { get; init; } [JsonPropertyName("reply_to")] public string[]? ReplyTo { get; init; } public string Subject { get; init; } = string.Empty; public string? Html { get; init; } public string? Text { get; init; } }
}
