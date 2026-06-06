namespace MassLab.Common.Api.Models;

/// <summary>
/// Represents a paginated response.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public class PagedResponse<T>
{
    private int _pageNumber = 1;
    private int _pageSize = 10;
    private int _totalCount;

    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Page number must be greater than 0.");
    }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Page size must be greater than 0.");
    }

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        set => _totalCount = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Total count cannot be negative.");
    }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Creates a successful paginated response.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="totalCount">The total number of items.</param>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A paginated response.</returns>
    public static PagedResponse<T> Create(
        IEnumerable<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        return new PagedResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Creates an empty paginated response.
    /// </summary>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>An empty paginated response.</returns>
    public static PagedResponse<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PagedResponse<T>
        {
            Items = [],
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
