using Amazon;
using Amazon.SimpleEmailV2;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Ses.Configuration;
using MassLab.Common.Email.Ses.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Ses.Extensions;

public static class SesEmailExtensions
{
    public static IServiceCollection AddSesEmail(this IServiceCollection services, IConfiguration? configuration = null, string sectionName = SesEmailOptions.SectionName)
    {
        var configured = new SesEmailOptions(); configuration?.GetSection(sectionName).Bind(configured); Validate(configured);
        if (configuration is not null) services.Configure<SesEmailOptions>(configuration.GetSection(sectionName)); else services.Configure<SesEmailOptions>(_ => { });
        services.AddSingleton<IAmazonSimpleEmailServiceV2>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SesEmailOptions>>().Value;
            var config = new AmazonSimpleEmailServiceV2Config { RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region) };
            return string.IsNullOrWhiteSpace(options.AccessKey) ? new AmazonSimpleEmailServiceV2Client(config) : new AmazonSimpleEmailServiceV2Client(options.AccessKey, options.SecretKey, config);
        });
        services.AddSingleton<IEmailSender, SesEmailSender>();
        services.AddSesEmailProviderFactory();
        return services;
    }
    public static IServiceCollection AddSesEmailProviderFactory(this IServiceCollection services) { services.AddSingleton<IEmailProviderSenderFactory, SesEmailProviderSenderFactory>(); return services; }
    private static void Validate(SesEmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Region)) throw new ArgumentException("Region is required.", nameof(options.Region));
        if (string.IsNullOrWhiteSpace(options.DefaultFrom)) throw new ArgumentException("Default sender is required.", nameof(options.DefaultFrom));
        if (string.IsNullOrWhiteSpace(options.AccessKey) != string.IsNullOrWhiteSpace(options.SecretKey)) throw new ArgumentException("AccessKey and SecretKey must be configured together.");
    }
}
