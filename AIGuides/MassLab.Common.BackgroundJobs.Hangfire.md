# MassLab.Common.BackgroundJobs.Hangfire

Use this for durable background jobs backed by Hangfire and optional dashboard.

NuGet:

```bash
dotnet add package MassLab.Common.BackgroundJobs.Hangfire
```

Setup:

```csharp
builder.Services.AddMassLabHangfire(builder.Configuration);
builder.Services.AddBackgroundJob<SendReceiptJob, SendReceipt>();

app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboardSafe("/jobs");
```

Config section: `BackgroundJobs`.

Read deeper only when needed: `MassLab.Common.BackgroundJobs.Hangfire/README.md`.

