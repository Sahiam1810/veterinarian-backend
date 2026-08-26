using Application.Security.Models;

namespace Application.Common.Results;
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
        {
            throw new ArgumentException(
                "The result state and error are inconsistent.",
                nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Create(bool isSuccess, Error error) =>
        new(isSuccess, error);

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? value;

    private Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        this.value = value;
    }

    public T Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Success(T value) =>
        new(value, true, Error.None);

    public static new Result<T> Failure(Error error) =>
        new(default, false, error);

    public static Result<AuthenticationTokens> Failure(object userAlreadyExists)
    {
        throw new NotImplementedException();
    }
}