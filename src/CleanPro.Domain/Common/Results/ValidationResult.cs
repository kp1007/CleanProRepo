namespace CleanPro.Domain.Common.Results;

public sealed class ValidationResult : Result
{
    private ValidationResult(Error[] errors)
        : base(false, errors.Length > 0 ? errors[0] : Error.None)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationResult WithErrors(Error[] errors) => new(errors);
}

public sealed class ValidationResult<T> : Result<T>
{
    private ValidationResult(Error[] errors)
        : base(errors.Length > 0 ? errors[0] : Error.None)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationResult<T> WithErrors(Error[] errors) => new(errors);
}

file class Result<T> : Result
{
    private readonly T? _value;

    protected Result(Error error) : base(false, error)
    {
        _value = default;
    }
}
