using FluentValidation;
using FluentValidation.Results;
using MediatR;
using MassLab.Common.Validation.Exceptions;

namespace MassLab.Common.Validation.Behaviors;

/// <summary>
/// MediatR pipeline behavior that executes FluentValidation validators before the request handler.
/// If validation fails, throws a ValidationException and prevents the handler from executing.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The collection of validators for the request type.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Handles the request by executing all validators before passing to the next handler.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the handler.</returns>
    /// <exception cref="Exceptions.ValidationException">Thrown when validation fails.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If no validators are registered, skip validation
        if (!_validators.Any())
        {
            return await next();
        }

        // Create validation context
        var context = new ValidationContext<TRequest>(request);

        // Execute all validators in parallel
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Collect all validation failures
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // If there are failures, throw ValidationException
        if (failures.Any())
        {
            throw new Exceptions.ValidationException(failures);
        }

        // Proceed to the next handler
        return await next();
    }
}
