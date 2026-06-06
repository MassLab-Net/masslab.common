using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Victor.Common.Validation.Behaviors;

namespace Victor.Common.Validation.Extensions;

/// <summary>
/// Extension methods for registering validation services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers FluentValidation validators and the validation pipeline behavior from the specified assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly containing validators.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddValidation(
        this IServiceCollection services,
        Assembly assembly)
    {
        // Register all validators from the specified assembly
        services.AddValidatorsFromAssembly(assembly);

        // Register ValidationBehavior as a MediatR pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }

    /// <summary>
    /// Registers FluentValidation validators and the validation pipeline behavior from multiple assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies containing validators.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddValidation(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // Register all validators from the specified assemblies
        foreach (var assembly in assemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        // Register ValidationBehavior as a MediatR pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
