using Angur.Domain.Abstractions;

namespace Angur.Domain.Common;

public static class MoneyErrors
{
    public static readonly DomainError InvalidScale = new("Money.InvalidScale", "Amount has more decimal places than the currency allows.", ErrorType.Validation);
}