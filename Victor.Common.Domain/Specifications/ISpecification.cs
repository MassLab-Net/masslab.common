using System.Linq.Expressions;

namespace Victor.Common.Domain.Specifications;

/// <summary>
/// Specification pattern: encapsulates a filter, includes, ordering and
/// paging that an <c>IReadRepository&lt;T&gt;</c> can evaluate against the
/// underlying data store.
/// </summary>
/// <typeparam name="T">The entity type the specification is built for.</typeparam>
public interface ISpecification<T>
{
    /// <summary>Filter predicate (mandatory, may be <c>x => true</c>).</summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>Eager-load expressions (e.g. <c>order =&gt; order.Items</c>).</summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>Property to order by (optional).</summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>True if ordering is descending.</summary>
    bool OrderDescending { get; }

    /// <summary>Number of rows to skip (paging).</summary>
    int? Skip { get; }

    /// <summary>Maximum number of rows to take (paging).</summary>
    int? Take { get; }

    /// <summary>True when EFCore should use <c>AsSplitQuery()</c>.</summary>
    bool AsSplitQuery { get; }

    /// <summary>True when EFCore should use <c>AsNoTracking()</c>.</summary>
    bool AsNoTracking { get; }
}

/// <summary>
/// Convenient base implementation of <see cref="ISpecification{T}"/> using a
/// fluent builder. Subclass and call the protected helpers from your ctor.
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = new();

    /// <inheritdoc />
    public Expression<Func<T, bool>> Criteria { get; private set; } = _ => true;

    /// <inheritdoc />
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public bool OrderDescending { get; private set; }

    /// <inheritdoc />
    public int? Skip { get; private set; }

    /// <inheritdoc />
    public int? Take { get; private set; }

    /// <inheritdoc />
    public bool AsSplitQuery { get; private set; }

    /// <inheritdoc />
    public bool AsNoTracking { get; private set; } = true;

    /// <summary>Creates a specification with the given filter.</summary>
    protected Specification(Expression<Func<T, bool>> criteria) =>
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));

    /// <summary>Creates an "all rows" specification.</summary>
    protected Specification() { }

    /// <summary>Adds an include expression.</summary>
    protected void AddInclude(Expression<Func<T, object>> include)
    {
        ArgumentNullException.ThrowIfNull(include);
        _includes.Add(include);
    }

    /// <summary>Sets ascending ordering.</summary>
    protected void ApplyOrderBy(Expression<Func<T, object>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        OrderBy = keySelector;
        OrderDescending = false;
    }

    /// <summary>Sets descending ordering.</summary>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        OrderBy = keySelector;
        OrderDescending = true;
    }

    /// <summary>Sets paging.</summary>
    protected void ApplyPaging(int skip, int take)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip), skip, "Skip cannot be negative.");
        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be greater than zero.");

        Skip = skip;
        Take = take;
    }

    /// <summary>Enables EFCore <c>AsSplitQuery()</c>.</summary>
    protected void UseSplitQuery() => AsSplitQuery = true;

    /// <summary>Enables EFCore tracking (default is no-tracking).</summary>
    protected void EnableTracking() => AsNoTracking = false;
}
