using Angur.Domain.Abstractions;

namespace Angur.Domain.Common;

public static class CurrencyErrors
{
    public static readonly DomainError Empty = new("Currency.Empty", "The currency is empty.", ErrorType.Validation);
    public static readonly DomainError Unsupported = new("Currency.Unsupported", "This currency is not supported.", ErrorType.Validation);
}
