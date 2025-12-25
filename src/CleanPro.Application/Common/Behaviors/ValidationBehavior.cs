using CleanPro.Application.Common.Interfaces;
using CleanPro.Domain.Common.Results;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanPro.Application.Common.Behaviors;

public sealed class ValidationBehavior<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _next;
    private readonly IEnumerable<IValidator<TCommand>> _validators;

    public ValidationBehavior(
        ICommandHandler<TCommand, TResult> next,
        IEnumerable<IValidator<TCommand>> validators)
    {
        _next = next;
        _validators = validators;
    }

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (!_validators.Any())
        {
            return await _next.HandleAsync(command, cancellationToken);
        }

        var context = new ValidationContext<TCommand>(command);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var errors = failures
                .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
                .ToArray();

            if (typeof(TResult).IsGenericType &&
                typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResult);
                var innerType = resultType.GetGenericArguments()[0];
                var validationResultType = typeof(ValidationResult<>).MakeGenericType(innerType);
                var withErrorsMethod = validationResultType.GetMethod("WithErrors");
                var result = withErrorsMethod!.Invoke(null, [errors]);
                return (TResult)result!;
            }

            throw new ValidationException(failures);
        }

        return await _next.HandleAsync(command, cancellationToken);
    }
}

public static class ValidationBehaviorExtensions
{
    public static IServiceCollection DecorateWithValidation<TCommand, TResult>(
        this IServiceCollection services)
        where TCommand : ICommand<TCommand, TResult>
    {
        services.Decorate<ICommandHandler<TCommand, TResult>>(
            (inner, sp) => new ValidationBehavior<TCommand, TResult>(
                inner,
                sp.GetServices<IValidator<TCommand>>()));

        return services;
    }
}
