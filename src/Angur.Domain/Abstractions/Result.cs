namespace Angur.Domain.Abstractions;

public class Result
{
    protected Result(bool isSuccess, DomainError error)
    {
        if (isSuccess && error != DomainError.None)
            throw new ArgumentException("A successful result cannot carry an error.", nameof(error));

        if (!isSuccess && error == DomainError.None)
            throw new ArgumentException("A failed result must carry an error.", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public DomainError Error { get; }

    public static Result Success() => new Result(true, DomainError.None);
    public static Result Failure(DomainError error) => new Result(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new Result<TValue>(true, DomainError.None, value);
    public static Result<TValue> Failure<TValue>(DomainError error) => new Result<TValue>(false, error, default);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;
    internal Result(bool isSuccess, DomainError error, TValue? value) : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");
}
