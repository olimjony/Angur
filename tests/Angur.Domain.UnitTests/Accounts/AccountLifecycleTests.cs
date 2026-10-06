using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Accounts.Events;
using Angur.Domain.Common;

using static Angur.Domain.UnitTests.Accounts.AccountTestData;

namespace Angur.Domain.UnitTests.Accounts;

public class AccountLifecycleTests
{
    // Accounts are opened at Now; lifecycle actions happen later.
    private static readonly DateTimeOffset Later = Now.AddDays(1);
    private static readonly DateTimeOffset EvenLater = Now.AddDays(2);

    // ---------- Freeze ----------

    [Fact]
    public void Freeze_ActiveAccount_FreezesWithTrimmedReasonAndTime()
    {
        // Arrange
        Account account = TjsAccountWithBalance(100m);

        // Act
        Result result = account.Freeze("  Court order 15/2026  ", Later);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Frozen, account.Status);
        Assert.Equal(Later, account.FrozenAt);
        Assert.Equal("Court order 15/2026", account.FreezeReason);
    }

    [Fact]
    public void Freeze_ActiveAccount_RaisesAccountFrozenWithTrimmedReason()
    {
        Account account = TjsAccountWithBalance(100m);

        account.Freeze("  Court order 15/2026  ", Later);

        IDomainEvent domainEvent = Assert.Single(account.GetDomainEvents());
        AccountFrozen frozen = Assert.IsType<AccountFrozen>(domainEvent);
        Assert.Equal(account.Id, frozen.AccountId);
        Assert.Equal("Court order 15/2026", frozen.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Freeze_WithoutReason_ReturnsFreezeReasonRequiredAndChangesNothing(string? reason)
    {
        // Arrange
        Account account = TjsAccountWithBalance(100m);
        Snapshot before = Snapshot.Of(account);

        // Act
        Result result = account.Freeze(reason, Later);

        // Assert
        Assert.Equal(AccountErrors.FreezeReasonRequired, result.Error);
        before.AssertUnchanged(account);
    }

    [Fact]
    public void Freeze_AlreadyFrozen_ReturnsAccountAlreadyFrozenAndChangesNothing()
    {
        // Arrange
        Account account = FrozenAccount();
        Snapshot before = Snapshot.Of(account);

        // Act
        Result result = account.Freeze("Another reason", EvenLater);

        // Assert
        Assert.Equal(AccountErrors.AccountAlreadyFrozen, result.Error);
        before.AssertUnchanged(account);
    }

    [Fact]
    public void Freeze_AlreadyFrozenAndNoReason_ReportsAlreadyFrozen()
    {
        // The state problem is more important than the missing reason.
        Account account = FrozenAccount();

        Result result = account.Freeze(null, EvenLater);

        Assert.Equal(AccountErrors.AccountAlreadyFrozen, result.Error);
    }

    [Fact]
    public void Freeze_ClosedAccount_ReturnsAccountClosedAndChangesNothing()
    {
        Account account = ClosedAccount();
        Snapshot before = Snapshot.Of(account);

        Result result = account.Freeze("Court order", EvenLater);

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        before.AssertUnchanged(account);
    }

    // ---------- Unfreeze ----------

    [Fact]
    public void Unfreeze_FrozenAccount_BecomesActiveAndClearsFreezeData()
    {
        // Arrange
        Account account = FrozenAccount();

        // Act
        Result result = account.Unfreeze();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Null(account.FrozenAt);
        Assert.Null(account.FreezeReason);
    }

    [Fact]
    public void Unfreeze_FrozenAccount_RaisesAccountUnfrozen()
    {
        Account account = FrozenAccount();

        account.Unfreeze();

        IDomainEvent domainEvent = Assert.Single(account.GetDomainEvents());
        AccountUnfrozen unfrozen = Assert.IsType<AccountUnfrozen>(domainEvent);
        Assert.Equal(account.Id, unfrozen.AccountId);
    }

    [Fact]
    public void Unfreeze_ActiveAccount_ReturnsAccountNotFrozenAndChangesNothing()
    {
        Account account = TjsAccountWithBalance(100m);
        Snapshot before = Snapshot.Of(account);

        Result result = account.Unfreeze();

        Assert.Equal(AccountErrors.AccountNotFrozen, result.Error);
        before.AssertUnchanged(account);
    }

    [Fact]
    public void Unfreeze_ClosedAccount_ReturnsAccountClosedAndChangesNothing()
    {
        Account account = ClosedAccount();
        Snapshot before = Snapshot.Of(account);

        Result result = account.Unfreeze();

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        before.AssertUnchanged(account);
    }

    [Fact]
    public void FreezeUnfreezeFreeze_WorksRepeatedly()
    {
        // Active <-> Frozen is reversible as many times as needed.
        Account account = TjsAccountWithBalance(100m);

        Assert.True(account.Freeze("Suspicious activity", Later).IsSuccess);
        Assert.True(account.Unfreeze().IsSuccess);
        Assert.True(account.Freeze("Court order", EvenLater).IsSuccess);

        Assert.Equal(AccountStatus.Frozen, account.Status);
        Assert.Equal("Court order", account.FreezeReason);
        Assert.Equal(EvenLater, account.FrozenAt);
    }

    // ---------- Close ----------

    [Fact]
    public void Close_ActiveAccountWithZeroBalance_ClosesWithTime()
    {
        // Arrange
        Account account = TjsAccountWithBalance(0m);

        // Act
        Result result = account.Close(Later);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.Equal(Later, account.ClosedAt);
    }

    [Fact]
    public void Close_ActiveAccountWithZeroBalance_RaisesAccountClosed()
    {
        Account account = TjsAccountWithBalance(0m);

        account.Close(Later);

        IDomainEvent domainEvent = Assert.Single(account.GetDomainEvents());
        AccountClosed closed = Assert.IsType<AccountClosed>(domainEvent);
        Assert.Equal(account.Id, closed.AccountId);
    }

    [Fact]
    public void Close_WithFunds_ReturnsNonZeroBalanceAndChangesNothing()
    {
        Account account = TjsAccountWithBalance(0.01m);
        Snapshot before = Snapshot.Of(account);

        Result result = account.Close(Later);

        Assert.Equal(AccountErrors.NonZeroBalance, result.Error);
        before.AssertUnchanged(account);
    }

    [Fact]
    public void Close_AfterWithdrawingEverything_Succeeds()
    {
        // The real-life path: take the money out, then close.
        Account account = TjsAccountWithBalance(250m);

        account.Withdraw(Tjs(250m));
        Result result = account.Close(Later);

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Closed, account.Status);
    }

    [Fact]
    public void Close_FrozenAccount_ReturnsAccountFrozenAndChangesNothing()
    {
        Account account = FrozenAccount(balance: 0m);
        Snapshot before = Snapshot.Of(account);

        Result result = account.Close(EvenLater);

        Assert.Equal(AccountErrors.AccountFrozen, result.Error);
        before.AssertUnchanged(account);
    }

    [Fact]
    public void Close_AlreadyClosed_ReturnsAccountClosedAndKeepsOriginalCloseTime()
    {
        Account account = ClosedAccount();
        Snapshot before = Snapshot.Of(account);

        Result result = account.Close(EvenLater);

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        before.AssertUnchanged(account);
    }

    // ---------- Money operations in each state ----------

    [Fact]
    public void Deposit_FrozenAccount_Succeeds()
    {
        // Freezing blocks debits only; incoming money (e.g. salary) is still accepted.
        Account account = FrozenAccount(balance: 100m);

        Result result = account.Deposit(Tjs(50m));

        Assert.True(result.IsSuccess);
        Assert.Equal(Tjs(150m), account.Balance);
    }

    [Fact]
    public void Withdraw_FrozenAccount_ReturnsAccountFrozenAndChangesNothing()
    {
        Account account = FrozenAccount(balance: 100m);
        Snapshot before = Snapshot.Of(account);

        Result result = account.Withdraw(Tjs(10m));

        Assert.Equal(AccountErrors.AccountFrozen, result.Error);
        before.AssertUnchanged(account);
    }

    [Fact]
    public void Deposit_ClosedAccount_ReturnsAccountClosedAndChangesNothing()
    {
        Account account = ClosedAccount();
        Snapshot before = Snapshot.Of(account);

        Result result = account.Deposit(Tjs(10m));

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        before.AssertUnchanged(account);
    }

    [Fact]
    public void Withdraw_ClosedAccount_ReturnsAccountClosedAndChangesNothing()
    {
        Account account = ClosedAccount();
        Snapshot before = Snapshot.Of(account);

        Result result = account.Withdraw(Tjs(10m));

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        before.AssertUnchanged(account);
    }

    // ---------- Time must not go backwards (programmer error) ----------

    [Fact]
    public void Freeze_BeforeAccountWasOpened_ThrowsArgumentOutOfRangeException()
    {
        Account account = TjsAccountWithBalance(0m);

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Freeze("Court order", Now.AddSeconds(-1)));
    }

    [Fact]
    public void Close_BeforeAccountWasOpened_ThrowsArgumentOutOfRangeException()
    {
        Account account = TjsAccountWithBalance(0m);

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Close(default));
    }

    [Fact]
    public void Close_AtTheSameMomentAsOpened_Succeeds()
    {
        // "Not before opening" means equal is fine.
        Account account = TjsAccountWithBalance(0m);

        Result result = account.Close(Now);

        Assert.True(result.IsSuccess);
    }

    // ---------- Helpers ----------

    private static Account FrozenAccount(decimal balance = 100m)
    {
        Account account = TjsAccountWithBalance(balance);
        account.Freeze("Suspicious activity", Later);
        account.ClearDomainEvents();
        return account;
    }

    private static Account ClosedAccount()
    {
        Account account = TjsAccountWithBalance(0m);
        account.Close(Later);
        account.ClearDomainEvents();
        return account;
    }

    /// <summary>Everything a failed operation must leave untouched.</summary>
    private sealed record Snapshot(
        AccountStatus Status,
        Money Balance,
        DateTimeOffset? FrozenAt,
        string? FreezeReason,
        DateTimeOffset? ClosedAt)
    {
        public static Snapshot Of(Account account) =>
            new(account.Status, account.Balance, account.FrozenAt, account.FreezeReason, account.ClosedAt);

        public void AssertUnchanged(Account account)
        {
            Assert.Equal(this, Of(account));
            Assert.Empty(account.GetDomainEvents());
        }
    }
}
