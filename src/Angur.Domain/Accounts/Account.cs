using Angur.Domain.Abstractions;
using Angur.Domain.Accounts.Events;
using Angur.Domain.Common;
using Angur.Domain.Customers;

namespace Angur.Domain.Accounts;

public sealed class Account : AggregateRoot<AccountId>
{
    private Account(AccountId id, CustomerId customerId, AccountNumber accountNumber, Currency currency, DateTimeOffset openedAt) : base(id)
    {
        CustomerId = customerId;
        Number = accountNumber;
        Balance = Money.Zero(currency);
        OpenedAt = openedAt;
        Status = AccountStatus.Active;
    }

    public CustomerId CustomerId { get; private set; }
    public AccountNumber Number { get; private set; }
    public Money Balance { get; private set; }
    public Currency Currency => Balance.Currency;
    public AccountStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }

    public static Result<Account> Open(Customer owner, AccountNumber number, Currency currency, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(currency);

        if (!owner.IsKycVerified)
        {
            return Result.Failure<Account>(AccountErrors.CustomerNotVerified);
        }

        Account account = new(AccountId.New(), owner.Id, number, currency, now);

        account.Raise(new AccountOpened(account.Id, account.CustomerId));

        return Result.Success(account);
    }
}
