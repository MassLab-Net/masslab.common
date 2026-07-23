# MassLab.Common.Email

Provider-agnostic email contracts. Add a sender provider (`Resend` or `Smtp`) and, for local templates, `MassLab.Common.Email.Templates.FileSystem`.

Use `IEmailSender` for single or batch submission. Register `IEmailDeliveryEventHandler` implementations to persist verified provider lifecycle events.

```csharp
var result = await emailSender.SendAsync(new EmailSendRequest
{
    To = [new EmailAddress("customer@example.com", "Customer")],
    Content = new LocalTemplateEmailContent("order-confirmation", new { orderNumber = "A-42" }),
    CorrelationId = order.Id.ToString(),
    IdempotencyKey = $"order-confirmation/{order.Id}",
    IncludeRenderedContent = true
}, cancellationToken);
```

`IncludeRenderedContent` is opt-in because bodies can contain PII, links, or one-time tokens. Store the returned snapshot and consume lifecycle events in the application database when building an audit/history view.
