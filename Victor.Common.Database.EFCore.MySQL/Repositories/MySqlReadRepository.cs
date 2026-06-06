using Microsoft.EntityFrameworkCore;
using Victor.Common.Database.EFCore.Repositories;

namespace Victor.Common.Database.EFCore.MySQL.Repositories;

/// <summary>
/// MySQL implementation of read repository for Entity Framework Core.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class MySqlReadRepository<TEntity>(DbContext context) : EFCoreReadRepository<TEntity>(context)
    where TEntity : class
{
}
