using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Templates.FileSystem.Configuration;
using MassLab.Common.Email.Templates.FileSystem.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MassLab.Common.Email.Templates.FileSystem.Extensions;

public static class FileSystemEmailTemplateExtensions
{
    public static IServiceCollection AddFileSystemEmailTemplates(this IServiceCollection services, IConfiguration? configuration = null, string sectionName = FileSystemEmailTemplateOptions.SectionName)
    {
        var configured = new FileSystemEmailTemplateOptions();
        configuration?.GetSection(sectionName).Bind(configured);
        if (string.IsNullOrWhiteSpace(configured.RootPath)) throw new ArgumentException("Root path is required.", nameof(configured.RootPath));
        if (configuration is not null) services.Configure<FileSystemEmailTemplateOptions>(configuration.GetSection(sectionName));
        else services.Configure<FileSystemEmailTemplateOptions>(_ => { });
        services.AddSingleton<IEmailTemplateStore, FileSystemEmailTemplateStore>();
        services.AddSingleton<IEmailTemplateRenderer, HandlebarsEmailTemplateRenderer>();
        return services;
    }

    /// <summary>Registers file-system templates with explicitly configured options.</summary>
    public static IServiceCollection AddFileSystemEmailTemplates(this IServiceCollection services, Action<FileSystemEmailTemplateOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var configured = new FileSystemEmailTemplateOptions();
        configure(configured);
        if (string.IsNullOrWhiteSpace(configured.RootPath)) throw new ArgumentException("Root path is required.", nameof(configured.RootPath));
        services.Configure(configure);
        services.AddSingleton<IEmailTemplateStore, FileSystemEmailTemplateStore>();
        services.AddSingleton<IEmailTemplateRenderer, HandlebarsEmailTemplateRenderer>();
        return services;
    }
}
