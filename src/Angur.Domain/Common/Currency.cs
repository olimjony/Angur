using System.Collections.Frozen;
using System.Collections.Immutable;

using Angur.Domain.Abstractions;

namespace Angur.Domain.Common;

public sealed record Currency
{
    public static readonly Currency TJS = new("TJS", 2);
    public static readonly Currency USD = new("USD", 2);
    public static readonly Currency EUR = new("EUR", 2);
    public static readonly Currency RUB = new("RUB", 2);

    public string Code { get; }
    public int MinorUnits { get; }

    private Currency(string code, int minorUnits)
    {
        Code = code;
        MinorUnits = minorUnits;
    }

    private static readonly ImmutableArray<Currency> Supported = [TJS, USD, EUR, RUB];

    private static readonly FrozenDictionary<string, Currency> ByCode =
        Supported.ToFrozenDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<Currency> All => Supported;

    public static Result<Currency> FromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Currency>(CurrencyErrors.Empty);
        }

        return ByCode.TryGetValue(code.Trim(), out Currency? currency)
            ? Result.Success(currency)
            : Result.Failure<Currency>(CurrencyErrors.Unsupported);
    }

    public override string ToString() => Code;
}
