namespace Victor.Common.Api.Models;

/// <summary>
/// Interface for paginated requests.
/// All requests that require pagination should implement this interface.
/// </summary>
public interface IPagedRequest
{
    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    int PageNumber { get; }

    /// <summary>
    /// Gets the page size (number of items per page).
    /// </summary>
    int PageSize { get; }

    /// <summary>
    /// Gets the number of items to skip.
    /// </summary>
    int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Gets the number of items to take.
    /// </summary>
    int Take => PageSize;
}
