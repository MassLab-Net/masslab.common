namespace MassLab.Common.Email.Models;

public enum EmailProviderKind { Smtp, Resend, Ses }

public abstract record EmailProviderConfiguration(EmailProviderKind Provider, string DefaultFrom);

public sealed record SmtpEmailProviderConfiguration(string DefaultFrom, string Host, int Port, bool UseSsl, string? UserName, string? Password)
    : EmailProviderConfiguration(EmailProviderKind.Smtp, DefaultFrom);

public sealed record ResendEmailProviderConfiguration(string DefaultFrom, string ApiKey, string BaseUrl = "https://api.resend.com")
    : EmailProviderConfiguration(EmailProviderKind.Resend, DefaultFrom);

public sealed record SesEmailProviderConfiguration(string DefaultFrom, string Region, string? AccessKey, string? SecretKey, string? ConfigurationSetName)
    : EmailProviderConfiguration(EmailProviderKind.Ses, DefaultFrom);
