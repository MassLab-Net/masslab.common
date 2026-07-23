using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Extensions;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Resend.Extensions;
using MassLab.Common.Email.Ses.Extensions;
using MassLab.Common.Email.Smtp.Extensions;
using MassLab.Common.Email.Templates.FileSystem.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables(prefix: "MASSLAB_")
    .AddCommandLine(args)
    .Build();

var providerName = configuration["Email:Provider"]?.Trim().ToLowerInvariant();
var services = new ServiceCollection();
services.AddMassLabEmailCore();
services.AddFileSystemEmailTemplates(options =>
{
    var configuredRoot = configuration["Email:Templates:RootPath"] ?? "Templates";
    options.RootPath = Path.IsPathRooted(configuredRoot)
        ? configuredRoot
        : Path.Combine(AppContext.BaseDirectory, configuredRoot);
    options.ReloadOnChange = bool.TryParse(configuration["Email:Templates:ReloadOnChange"], out var reloadOnChange) && reloadOnChange;
});

switch (providerName)
{
    case "resend": services.AddResendEmail(configuration); break;
    case "ses": services.AddSesEmail(configuration); break;
    case "smtp": services.AddSmtpEmail(configuration); break;
    default: throw new InvalidOperationException("Email:Provider must be Resend, Ses, or Smtp.");
}

using var serviceProvider = services.BuildServiceProvider();
var sender = serviceProvider.GetRequiredService<IEmailSender>();
var recipient = configuration["Email:Example:Recipient"] ?? throw new InvalidOperationException("Email:Example:Recipient is required.");
var mode = configuration["Email:Example:ContentMode"]?.Trim().ToLowerInvariant() ?? "local";
var request = new EmailSendRequest
{
    To = [new EmailAddress(recipient)],
    Content = mode switch
    {
        "local" => new LocalTemplateEmailContent(configuration["Email:Example:LocalTemplateKey"] ?? "welcome", new
        {
            name = configuration["Email:Example:Name"] ?? "MassLab developer",
            verificationUrl = configuration["Email:Example:VerificationUrl"] ?? "https://example.test/verify"
        }),
        "provider" => new ProviderTemplateEmailContent(
            configuration["Email:Example:ProviderTemplateName"] ?? throw new InvalidOperationException("Email:Example:ProviderTemplateName is required for provider mode."),
            new Dictionary<string, object?>
            {
                ["name"] = configuration["Email:Example:Name"] ?? "MassLab developer",
                ["verificationUrl"] = configuration["Email:Example:VerificationUrl"] ?? "https://example.test/verify"
            }),
        _ => throw new InvalidOperationException("Email:Example:ContentMode must be Local or Provider.")
    },
    CorrelationId = $"console-sample-{Guid.NewGuid():N}",
    IdempotencyKey = $"console-sample/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
    IncludeRenderedContent = true
};

var result = await sender.SendAsync(request);
Console.WriteLine($"Provider: {result.Provider}");
Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Message ID: {result.ProviderMessageId ?? "(none)"}");
if (!result.IsAccepted) Console.WriteLine($"Error: {result.ErrorCode}: {result.ErrorMessage}");
else if (result.Message is not null) Console.WriteLine($"Subject: {result.Message.Subject}");

Environment.ExitCode = result.IsAccepted ? 0 : 1;
