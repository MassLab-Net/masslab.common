using Microsoft.EntityFrameworkCore;
using MassLab.Common.Domain.Specifications;

namespace MassLab.Common.Database.EFCore.Specifications;

/// <summary>
/// Translates an <see cref="ISpecification{T}"/> into an
/// <see cref="IQueryable{T}"/> using EFCore primitives.
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>Builds an <see cref="IQueryable{T}"/> from the supplied specification.</summary>
    public static IQueryable<T> Apply<T>(IQueryable<T> source, ISpecification<T> spec)
        where T : class
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (spec is null)   throw new ArgumentNullException(nameof(spec));

        var query = source;

        if (spec.AsNoTracking)  query = query.AsNoTracking();
        if (spec.AsSplitQuery)  query = query.AsSplitQuery();

        query = query.Where(spec.Criteria);

        foreach (var include in spec.Includes)
            query = query.Include(include);

        if (spec.OrderBy is not null)
        {
            query = spec.OrderDescending
                ? query.OrderByDescending(spec.OrderBy)
                : query.OrderBy(spec.OrderBy);
        }

        if (spec.Skip is int s) query = query.Skip(s);
        if (spec.Take is int t) query = query.Take(t);

        return query;
    }
}
