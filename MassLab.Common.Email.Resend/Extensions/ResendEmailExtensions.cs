using System.Net.Http.Headers;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Resend.Configuration;
using MassLab.Common.Email.Resend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Resend.Extensions;

public static class ResendEmailExtensions
{
    public static IServiceCollection AddResendEmail(this IServiceCollection services, IConfiguration? configuration = null, string sectionName = ResendEmailOptions.SectionName)
    {
        var configured = new ResendEmailOptions();
        configuration?.GetSection(sectionName).Bind(configured);
        Validate(configured);
        if (configuration is not null) services.Configure<ResendEmailOptions>(configuration.GetSection(sectionName));
        else services.Configure<ResendEmailOptions>(_ => { });
        services.AddHttpClient<ResendEmailSender>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ResendEmailOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MassLab.Common.Email/1.0");
        });
        services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<ResendEmailSender>());
        services.AddSingleton<IEmailMessageReader>(sp => sp.GetRequiredService<ResendEmailSender>());
        return services;
    }

    public static IEndpointConventionBuilder MapResendEmailWebhooks(this IEndpointRouteBuilder endpoints, string pattern = "/webhooks/email/resend")
        => endpoints.MapPost(pattern, ResendWebhookEndpoint.HandleAsync);

    private static void Validate(ResendEmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey)) throw new ArgumentException("API key is required.", nameof(options.ApiKey));
        if (string.IsNullOrWhiteSpace(options.DefaultFrom)) throw new ArgumentException("Default sender is required.", nameof(options.DefaultFrom));
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _)) throw new ArgumentException("Base URL must be absolute.", nameof(options.BaseUrl));
    }
}
