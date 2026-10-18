namespace Starter.Domain.Common;

public sealed class Result
{
    private static readonly Result SuccessResult = new(error: null);

    private readonly Error? _error;

    private Result(Error? error) => _error = error;

    public bool IsSuccess => _error is null;

    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? throw new InvalidOperationException("Cannot access the error of a successful result.");

    public static Result Success() => SuccessResult;

    public static Result Failure(Error error) => new(error);

#pragma warning disable CA2225 // Implicit conversion keeps returns readable; Failure() is the named alternative.
    public static implicit operator Result(Error error) => new(error);
#pragma warning restore CA2225
}
