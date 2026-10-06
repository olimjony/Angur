using System.Globalization;

using Angur.Domain.Abstractions;

namespace Angur.Domain.Common;

public sealed record Money
{
    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public Currency Currency { get; }

    public bool IsZero => Amount == 0m;
    public bool IsPositive => Amount > 0m;
    public bool IsNegative => Amount < 0m;

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        decimal normalized = decimal.Round(amount, currency.MinorUnits);

        if (amount != normalized)
        {
            return Result.Failure<Money>(MoneyErrors.InvalidScale);
        }

        return Result.Success(new Money(amount, currency));
    }

    public static Money Zero(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return new Money(0m, currency);
    }

    public static Money operator +(Money left, Money right)
    {
        EnsureNoneNotNull(left, right);
        EnsureSameCurrency(left, right);

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureNoneNotNull(left, right);
        EnsureSameCurrency(left, right);

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static bool operator >(Money left, Money right)
    {
        EnsureNoneNotNull(left, right);
        EnsureSameCurrency(left, right);

        return left.Amount > right.Amount;
    }
    public static bool operator <(Money left, Money right)
    {
        EnsureNoneNotNull(left, right);
        EnsureSameCurrency(left, right);

        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureNoneNotNull(left, right);
        EnsureSameCurrency(left, right);

        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureNoneNotNull(left, right);
        EnsureSameCurrency(left, right);

        return left.Amount <= right.Amount;
    }

    public override string ToString() => $"{Amount.ToString($"F{Currency.MinorUnits}", CultureInfo.InvariantCulture)} {Currency.Code}";

    private static void EnsureNoneNotNull(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
    }
    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException(
                $"Cannot operate on different currencies: {left.Currency} and {right.Currency}.");
    }
}
