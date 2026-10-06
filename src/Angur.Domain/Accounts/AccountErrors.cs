using Angur.Domain.Abstractions;

namespace Angur.Domain.Accounts;

public static class AccountErrors
{
    public static readonly DomainError CustomerNotVerified = new("Account.CustomerNotVerified", "Customer is not verified", ErrorType.Conflict);
    public static readonly DomainError AccountClosed = new("Account.AccountClosed", "The account is closed.", ErrorType.Conflict);
    public static readonly DomainError AccountFrozen = new("Account.AccountFrozen", "The account is frozen.", ErrorType.Conflict);
    public static readonly DomainError AmountMustBePositive = new("Account.AmountMustBePositive", "The amount of money must be positive", ErrorType.Validation);
    public static readonly DomainError CurrencyMismatch = new("Account.CurrencyMismatch", "Thee currency of deposit have to match the currency of account", ErrorType.Validation);
    public static readonly DomainError InsufficientFunds = new("Account.InsufficientFunds", "The Account has insifficinet funds for withdraw.", ErrorType.Failure);
}
