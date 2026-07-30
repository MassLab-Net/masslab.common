using Microsoft.Extensions.DependencyInjection;

namespace MassLab.Common.Email.Extensions;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddMassLabEmailCore(this IServiceCollection services) => services;
}
