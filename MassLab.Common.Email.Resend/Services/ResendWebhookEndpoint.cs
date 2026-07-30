using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Resend.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Resend.Services;

internal static class ResendWebhookEndpoint
{
    public static async Task<IResult> HandleAsync(HttpContext context, IOptions<ResendEmailOptions> options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.WebhookSecret)) return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Resend webhook secret is not configured.");
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var svixId = context.Request.Headers["svix-id"].ToString();
        var svixTimestamp = context.Request.Headers["svix-timestamp"].ToString();
        var svixSignature = context.Request.Headers["svix-signature"].ToString();
        if (!Verify(svixId, svixTimestamp, svixSignature, body, options.Value.WebhookSecret)) return Results.BadRequest();
        EmailLifecycleEvent emailEvent;
        try { emailEvent = Parse(svixId, body); }
        catch (JsonException) { return Results.BadRequest(); }
        foreach (var handler in context.RequestServices.GetServices<IEmailDeliveryEventHandler>()) await handler.HandleAsync(emailEvent, cancellationToken);
        return Results.Ok();
    }

    private static bool Verify(string id, string timestamp, string signatureHeader, string body, string secret)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signatureHeader)) return false;
        var encodedSecret = secret.StartsWith("whsec_", StringComparison.Ordinal) ? secret[6..] : secret;
        byte[] key;
        try { key = Convert.FromBase64String(encodedSecret); } catch (FormatException) { return false; }
        var payload = Encoding.UTF8.GetBytes($"{id}.{timestamp}.{body}");
        var expected = HMACSHA256.HashData(key, payload);
        foreach (var part in signatureHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var value = part.StartsWith("v1,", StringComparison.Ordinal) ? part[3..] : part;
            try { if (CryptographicOperations.FixedTimeEquals(expected, Convert.FromBase64String(value))) return true; }
            catch (FormatException) { }
        }
        return false;
    }

    private static EmailLifecycleEvent Parse(string eventId, string body)
    {
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var type = root.GetProperty("type").GetString() ?? throw new JsonException();
        var data = root.GetProperty("data");
        var providerMessageId = data.TryGetProperty("email_id", out var id) ? id.GetString() : null;
        var occurredAt = root.TryGetProperty("created_at", out var created) && DateTimeOffset.TryParse(created.GetString(), out var parsed) ? parsed : DateTimeOffset.UtcNow;
        var recipient = data.TryGetProperty("to", out var to) && to.ValueKind == JsonValueKind.Array && to.GetArrayLength() > 0 ? new EmailAddress(to[0].GetString() ?? string.Empty) : null;
        var tags = new Dictionary<string, string>();
        if (data.TryGetProperty("tags", out var tagValues) && tagValues.ValueKind == JsonValueKind.Object)
            foreach (var property in tagValues.EnumerateObject()) tags[property.Name] = property.Value.ToString();
        (EmailDeliveryStatus? delivery, EmailEngagementType? engagement) = type switch
        {
            "email.sent" => ((EmailDeliveryStatus?)EmailDeliveryStatus.Sent, null), "email.delivered" => ((EmailDeliveryStatus?)EmailDeliveryStatus.Delivered, null),
            "email.delivery_delayed" => ((EmailDeliveryStatus?)EmailDeliveryStatus.Delayed, null), "email.bounced" => ((EmailDeliveryStatus?)EmailDeliveryStatus.Bounced, null),
            "email.failed" => ((EmailDeliveryStatus?)EmailDeliveryStatus.Failed, null), "email.suppressed" => ((EmailDeliveryStatus?)EmailDeliveryStatus.Suppressed, null),
            "email.complained" => ((EmailDeliveryStatus?)EmailDeliveryStatus.Complained, null), "email.opened" => ((EmailDeliveryStatus?)null, (EmailEngagementType?)EmailEngagementType.Opened),
            "email.clicked" => ((EmailDeliveryStatus?)null, (EmailEngagementType?)EmailEngagementType.Clicked), _ => ((EmailDeliveryStatus?)null, (EmailEngagementType?)null)
        };
        return new EmailLifecycleEvent { EventId = eventId, Provider = "Resend", ProviderEventType = type, ProviderMessageId = providerMessageId, CorrelationId = tags.GetValueOrDefault("correlation_id"), OccurredAt = occurredAt, DeliveryStatus = delivery, EngagementType = engagement, Recipient = recipient, Reason = GetReason(data), Metadata = tags };
    }

    private static string? GetReason(JsonElement data) => data.TryGetProperty("bounce", out var bounce) && bounce.TryGetProperty("message", out var message) ? message.GetString() : null;
}
