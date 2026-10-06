using Angur.Domain.Abstractions;

namespace Angur.Domain.Accounts;

public sealed record AccountNumber
{
    public const int Length = 20;

    private AccountNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<AccountNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<AccountNumber>(AccountNumberErrors.Required);
        }

        string trimmed = value.Trim();

        if (trimmed.Length != Length)
        {
            return Result.Failure<AccountNumber>(AccountNumberErrors.InvalidLength);
        }

        if (!trimmed.All(char.IsAsciiDigit))
        {
            return Result.Failure<AccountNumber>(AccountNumberErrors.InvalidFormat);
        }

        return Result.Success(new AccountNumber(trimmed));
    }

    public override string ToString() => Value;
}
