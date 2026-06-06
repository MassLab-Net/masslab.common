using Microsoft.EntityFrameworkCore;
using MassLab.Common.Database.EFCore.Repositories;

namespace MassLab.Common.Database.EFCore.SqlServer.Repositories;

/// <summary>
/// SQL Server implementation of write repository for Entity Framework Core.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class SqlServerWriteRepository<TEntity>(DbContext context) : EFCoreWriteRepository<TEntity>(context)
    where TEntity : class
{
}
