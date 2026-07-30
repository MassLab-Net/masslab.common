# MassLab.Common.Email.Ses

Configure `Email:Ses` with `Region`, `DefaultFrom`, optional access keys, and optional `ConfigurationSetName`, then register `AddSesEmail()`.

`ProviderTemplateEmailContent` sends a stored SES template by name. Configure SES event destinations (SNS/EventBridge/CloudWatch) in AWS and ingest those events in the consumer service when delivery history is required.
