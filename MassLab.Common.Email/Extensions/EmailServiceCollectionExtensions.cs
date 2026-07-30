using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Services;

namespace MassLab.Common.Email.Extensions;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddMassLabEmailCore(this IServiceCollection services)
    {
        services.AddSingleton<IEmailSenderFactory, EmailSenderFactory>();
        return services;
    }
}
