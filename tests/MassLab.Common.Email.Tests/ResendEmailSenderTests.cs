using System.Net;
using System.Net.Http.Json;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Resend.Configuration;
using MassLab.Common.Email.Resend.Services;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Tests;

public class ResendEmailSenderTests
{
    [Fact]
    public async Task Sender_maps_raw_email_and_returns_provider_id()
    {
        string? payload = null;
        var client = new HttpClient(new StubHandler(async request =>
        {
            payload = await request.Content!.ReadAsStringAsync();
            request.Headers.GetValues("Idempotency-Key").Should().ContainSingle().Which.Should().Be("order/42");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = "email_123" }) };
        })) { BaseAddress = new Uri("https://example.test/") };
        var sender = new ResendEmailSender(client, Options.Create(new ResendEmailOptions { DefaultFrom = "MassLab <hello@example.test>" }));

        var result = await sender.SendAsync(new EmailSendRequest { To = [new EmailAddress("user@example.test")], Content = new RawEmailContent("Hello", "<p>Hi</p>"), CorrelationId = "history-1", IdempotencyKey = "order/42", IncludeRenderedContent = true });

        result.IsAccepted.Should().BeTrue();
        result.ProviderMessageId.Should().Be("email_123");
        result.Message!.Html.Should().Be("<p>Hi</p>");
        payload.Should().Contain("\"subject\":\"Hello\"");
    }

    [Fact]
    public async Task Batch_keeps_per_item_results_when_one_request_is_rejected()
    {
        var client = new HttpClient(new StubHandler(async request =>
        {
            var payload = await request.Content!.ReadAsStringAsync();
            return payload.Contains("bad")
                ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad request") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = "ok" }) };
        })) { BaseAddress = new Uri("https://example.test/") };
        var sender = new ResendEmailSender(client, Options.Create(new ResendEmailOptions { DefaultFrom = "hello@example.test" }));
        var requests = new[]
        {
            new EmailSendRequest { To = [new EmailAddress("a@example.test")], Content = new RawEmailContent("good", "ok") },
            new EmailSendRequest { To = [new EmailAddress("b@example.test")], Content = new RawEmailContent("bad", "no") }
        };

        var result = await sender.SendBatchAsync(requests);
        result.Results.Select(x => x.Status).Should().Equal(EmailSubmissionStatus.Accepted, EmailSubmissionStatus.Rejected);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => handler(request);
    }
}
