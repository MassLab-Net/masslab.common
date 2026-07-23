namespace MassLab.Common.Email.Ses.Configuration;

public sealed class SesEmailOptions
{
    public const string SectionName = "Email:Ses";
    public string Region { get; set; } = "us-east-1";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string DefaultFrom { get; set; } = string.Empty;
    public string? ConfigurationSetName { get; set; }
}
