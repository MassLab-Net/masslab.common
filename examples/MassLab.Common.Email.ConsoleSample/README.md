# MassLab Common Email Console Sample

1. Copy or edit `appsettings.json` and replace the recipient, sender, and credentials for the chosen provider.
2. Run a local Handlebars template with Resend:

```powershell
dotnet run --project examples/MassLab.Common.Email.ConsoleSample
```

3. Override a setting without committing credentials:

```powershell
$env:MASSLAB_Email__Provider = "Ses"
$env:MASSLAB_Email__Ses__DefaultFrom = "verified-sender@example.com"
$env:MASSLAB_Email__Example__Recipient = "your-inbox@example.com"
dotnet run --project examples/MassLab.Common.Email.ConsoleSample
```

Set `Email:Example:ContentMode` to `Provider` to send an SES template by name or a Resend template by ID/alias. SES requires the sender identity to be verified; accounts still in the SES sandbox can send only to verified recipients.
