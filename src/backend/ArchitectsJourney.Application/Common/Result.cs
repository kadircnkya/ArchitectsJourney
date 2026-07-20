namespace ArchitectsJourney.Application.Common;

/// <summary>
/// Discriminated union result type for operation outcomes.
/// Used throughout the Application layer to represent domain failures
/// without throwing exceptions. Exceptions are reserved for system faults.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed Result.");

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error of a successful Result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }

    public Result<TNew> Map<TNew>(Func<T, TNew> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return IsSuccess ? Result<TNew>.Success(transform(_value!)) : Result<TNew>.Failure(_error!);
    }

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}

/// <summary>
/// Non-generic result for operations that return no value on success.
/// </summary>
public readonly struct Result
{
    private readonly Error? _error;

    private Result(Error? error) => _error = error;

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;
    public Error Error => _error ?? throw new InvalidOperationException("Cannot access Error of a successful Result.");

    public static Result Success() => new(null);
    public static Result Failure(Error error) => new(error);
    public static implicit operator Result(Error error) => Failure(error);
}
