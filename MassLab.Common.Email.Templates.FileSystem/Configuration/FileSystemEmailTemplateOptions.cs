namespace MassLab.Common.Email.Templates.FileSystem.Configuration;

public sealed class FileSystemEmailTemplateOptions
{
    public const string SectionName = "Email:Templates";
    public string RootPath { get; set; } = "Templates";
    public bool ReloadOnChange { get; set; } = true;
}
