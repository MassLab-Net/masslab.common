using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Smtp.Configuration;
using MassLab.Common.Email.Smtp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MassLab.Common.Email.Smtp.Extensions;

public static class SmtpEmailExtensions
{
    public static IServiceCollection AddSmtpEmail(this IServiceCollection services, IConfiguration? configuration = null, string sectionName = SmtpEmailOptions.SectionName)
    {
        var configured = new SmtpEmailOptions(); configuration?.GetSection(sectionName).Bind(configured); Validate(configured);
        if (configuration is not null) services.Configure<SmtpEmailOptions>(configuration.GetSection(sectionName)); else services.Configure<SmtpEmailOptions>(_ => { });
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        return services;
    }
    private static void Validate(SmtpEmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host)) throw new ArgumentException("Host is required.", nameof(options.Host));
        if (options.Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(options.Port));
        if (string.IsNullOrWhiteSpace(options.DefaultFrom)) throw new ArgumentException("Default sender is required.", nameof(options.DefaultFrom));
        if (string.IsNullOrWhiteSpace(options.UserName) != string.IsNullOrWhiteSpace(options.Password)) throw new ArgumentException("UserName and Password must be configured together.");
    }
}
