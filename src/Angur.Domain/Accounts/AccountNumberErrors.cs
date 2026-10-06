using Angur.Domain.Abstractions;

namespace Angur.Domain.Accounts;

public static class AccountNumberErrors
{
    public static readonly DomainError Required = new("AccountNumber.Required", "Account number is required.", ErrorType.Validation);
    public static readonly DomainError InvalidLength = new("AccountNumber.InvalidLength", "Account number must be exactly 20 characters long.", ErrorType.Validation);
    public static readonly DomainError InvalidFormat = new("AccountNumber.InvalidFormat", "Account number must contain only digits.", ErrorType.Validation);
}
