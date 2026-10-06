using Angur.Domain.Abstractions;

namespace Angur.Domain.Accounts;

public static class AccountErrors
{
    public static readonly DomainError CustomerNotVerified = new("Account.CustomerNotVerified", "Customer is not verified", ErrorType.Conflict);
    public static readonly DomainError AccountClosed = new("Account.AccountClosed", "The account is closed.", ErrorType.Conflict);
    public static readonly DomainError AccountAlreadyClosed = new("Account.AccountAlreadyClosed", "The account is already closed.", ErrorType.Conflict);
    public static readonly DomainError AccountAlreadyFrozen = new("Account.AccountAlreadyFrozen", "The account is already frozen.", ErrorType.Conflict);
    public static readonly DomainError AccountFrozen = new("Account.AccountFrozen", "The account is frozen.", ErrorType.Conflict);
    public static readonly DomainError AccountNotFrozen = new("Account.AccountNotFrozen", "The account is not frozen.", ErrorType.Conflict);
    public static readonly DomainError FreezeReasonRequired = new("Account.FreezeReasonRequired", "The reason of freezing account is required", ErrorType.Validation);
    public static readonly DomainError AmountMustBePositive = new("Account.AmountMustBePositive", "The amount of money must be positive", ErrorType.Validation);
    public static readonly DomainError CurrencyMismatch = new("Account.CurrencyMismatch", "Thee currency of deposit have to match the currency of account", ErrorType.Validation);
    public static readonly DomainError InsufficientFunds = new("Account.InsufficientFunds", "The Account has insufficient funds for withdraw.", ErrorType.Failure);
    public static readonly DomainError NonZeroBalance = new("Account.NonZeroBalance", "The Account cannot be closed due to having funds.", ErrorType.Conflict);
    public static readonly DomainError NotFound = new("Account.NotFound", "Account was not found.", ErrorType.NotFound);

}
