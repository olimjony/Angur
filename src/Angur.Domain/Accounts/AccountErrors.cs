using Angur.Domain.Abstractions;

namespace Angur.Domain.Accounts;

public static class AccountErrors
{
    public static readonly DomainError CustomerNotVerified = new("Account.CustomerNotVerified", "Customer is not verified", ErrorType.Conflict);
}
