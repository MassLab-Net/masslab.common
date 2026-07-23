# MassLab.Common.Email.Resend

Configure `Email:Resend` with `ApiKey`, `DefaultFrom`, and `WebhookSecret`. Register with `AddResendEmail()` and expose verified lifecycle events through `MapResendEmailWebhooks()`.

Resend hosted templates are sent with `ProviderTemplateEmailContent(templateIdOrAlias, variables)`. When `IncludeRenderedContent` is enabled, the provider is queried best-effort for its rendered sent-email snapshot.
