using System.Runtime.InteropServices;

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
    public DateTimeOffset? FrozenAt { get; private set; }
    public string? FreezeReason { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

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

    public Result Deposit(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (Status == AccountStatus.Closed)
        {
            return Result.Failure(AccountErrors.AccountClosed);
        }

        if (!amount.IsPositive)
        {
            return Result.Failure(AccountErrors.AmountMustBePositive);
        }

        if (Currency != amount.Currency)
        {
            return Result.Failure(AccountErrors.CurrencyMismatch);
        }

        Balance += amount;

        Raise(new MoneyDeposited(Id, amount, Balance));

        return Result.Success();
    }

    public Result Withdraw(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (Status == AccountStatus.Closed)
        {
            return Result.Failure(AccountErrors.AccountClosed);
        }

        if (Status == AccountStatus.Frozen)
        {
            return Result.Failure(AccountErrors.AccountFrozen);
        }

        if (!amount.IsPositive)
        {
            return Result.Failure(AccountErrors.AmountMustBePositive);
        }

        if (Currency != amount.Currency)
        {
            return Result.Failure(AccountErrors.CurrencyMismatch);
        }

        if (Balance < amount)
        {
            return Result.Failure(AccountErrors.InsufficientFunds);
        }

        Balance -= amount;

        Raise(new MoneyWithdrawn(Id, amount, Balance));

        return Result.Success();
    }

    public Result Freeze(string? reason, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(now, OpenedAt);

        if (Status == AccountStatus.Closed)
        {
            return Result.Failure(AccountErrors.AccountClosed);
        }

        if (Status == AccountStatus.Frozen)
        {
            return Result.Failure(AccountErrors.AccountAlreadyFrozen);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(AccountErrors.FreezeReasonRequired);
        }

        Status = AccountStatus.Frozen;
        FrozenAt = now;
        FreezeReason = reason.Trim();

        Raise(new AccountFrozen(Id, FreezeReason));

        return Result.Success();
    }

    public Result Unfreeze()
    {
        if (Status == AccountStatus.Closed)
        {
            return Result.Failure(AccountErrors.AccountClosed);
        }

        if (Status == AccountStatus.Active)
        {
            return Result.Failure(AccountErrors.AccountNotFrozen);
        }

        Status = AccountStatus.Active;
        FrozenAt = null;
        FreezeReason = null;

        Raise(new AccountUnfrozen(Id));

        return Result.Success();
    }

    public Result Close(DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(now, OpenedAt);

        if (Status == AccountStatus.Closed)
        {
            return Result.Failure(AccountErrors.AccountClosed);
        }

        if (Status == AccountStatus.Frozen)
        {
            return Result.Failure(AccountErrors.AccountFrozen);
        }

        if (!Balance.IsZero)
        {
            return Result.Failure(AccountErrors.NonZeroBalance);
        }

        Status = AccountStatus.Closed;
        ClosedAt = now;

        Raise(new AccountClosed(Id));

        return Result.Success();
    }

}
