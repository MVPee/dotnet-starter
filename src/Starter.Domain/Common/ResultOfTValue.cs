namespace Starter.Domain.Common;

public sealed class Result<TValue>
{
    private readonly TValue? _value;
    private readonly Error? _error;

    private Result(TValue value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access the error of a successful result.");

    public TOutput Match<TOutput>(Func<TValue, TOutput> onSuccess, Func<Error, TOutput> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    public static Result<TValue> Success(TValue value) => new(value);

    public static Result<TValue> Failure(Error error) => new(error);

#pragma warning disable CA2225 // Implicit conversions keep use-case returns readable; named factories exist above.
    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator Result<TValue>(Error error) => new(error);
#pragma warning restore CA2225
}
