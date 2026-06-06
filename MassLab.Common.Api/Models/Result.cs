namespace MassLab.Common.Api.Models;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error message if the operation failed.</param>
    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Represents the result of an operation with a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="value">The value if the operation was successful.</param>
    /// <param name="error">The error message if the operation failed.</param>
    private Result(bool isSuccess, T? value, string error) : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful result with the value.</returns>
    public static Result<T> Success(T value) => new(true, value, string.Empty);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static new Result<T> Failure(string error) => new(false, default, error);
}
