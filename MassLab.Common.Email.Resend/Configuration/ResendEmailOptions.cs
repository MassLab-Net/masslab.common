namespace MassLab.Common.Email.Resend.Configuration;

public sealed class ResendEmailOptions
{
    public const string SectionName = "Email:Resend";
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultFrom { get; set; } = string.Empty;
    public string? WebhookSecret { get; set; }
    public string BaseUrl { get; set; } = "https://api.resend.com";
}
