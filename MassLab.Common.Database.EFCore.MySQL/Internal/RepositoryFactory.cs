using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.Database.Abstractions;
using MassLab.Common.Database.EFCore.MySQL.Repositories;

namespace MassLab.Common.Database.EFCore.MySQL.Internal;

/// <summary>
/// Factory for creating MySQL repository instances with separate read/write contexts.
/// </summary>
internal static class RepositoryFactory
{
    /// <summary>
    /// Creates a factory function for write repositories using a specific DbContext.
    /// </summary>
    /// <typeparam name="TWriteContext">The write DbContext type.</typeparam>
    /// <returns>A factory function that creates write repository instances.</returns>
    public static Func<IServiceProvider, Type, object> CreateWriteRepositoryFactory<TWriteContext>()
        where TWriteContext : DbContext
    {
        return (sp, entityType) =>
        {
            var context = sp.GetRequiredService<TWriteContext>();
            var repositoryType = typeof(MySqlWriteRepository<>).MakeGenericType(entityType);
            return Activator.CreateInstance(repositoryType, context)!;
        };
    }

    /// <summary>
    /// Creates a factory function for read repositories using a specific DbContext.
    /// </summary>
    /// <typeparam name="TReadContext">The read DbContext type.</typeparam>
    /// <returns>A factory function that creates read repository instances.</returns>
    public static Func<IServiceProvider, Type, object> CreateReadRepositoryFactory<TReadContext>()
        where TReadContext : DbContext
    {
        return (sp, entityType) =>
        {
            var context = sp.GetRequiredService<TReadContext>();
            var repositoryType = typeof(MySqlReadRepository<>).MakeGenericType(entityType);
            return Activator.CreateInstance(repositoryType, context)!;
        };
    }
}
