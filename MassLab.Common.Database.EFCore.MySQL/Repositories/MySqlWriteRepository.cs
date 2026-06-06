using Microsoft.EntityFrameworkCore;
using MassLab.Common.Database.EFCore.Repositories;

namespace MassLab.Common.Database.EFCore.MySQL.Repositories;

/// <summary>
/// MySQL implementation of write repository for Entity Framework Core.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class MySqlWriteRepository<TEntity>(DbContext context) : EFCoreWriteRepository<TEntity>(context)
    where TEntity : class
{
}
