using Microsoft.EntityFrameworkCore;
using MassLab.Common.Database.EFCore.Repositories;

namespace MassLab.Common.Database.EFCore.MySQL.Repositories;

/// <summary>
/// MySQL implementation of read repository for Entity Framework Core.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class MySqlReadRepository<TEntity>(DbContext context) : EFCoreReadRepository<TEntity>(context)
    where TEntity : class
{
}
