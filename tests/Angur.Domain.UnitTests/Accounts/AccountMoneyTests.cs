using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Accounts.Events;
using Angur.Domain.Common;

using static Angur.Domain.UnitTests.Accounts.AccountTestData;

namespace Angur.Domain.UnitTests.Accounts;

public class AccountMoneyTests
{
    public static TheoryData<decimal> NotPositiveAmounts => [0m, -0.01m, -100m];

    // ---------- Deposit: success ----------

    [Fact]
    public void Deposit_PositiveAmount_IncreasesBalance()
    {
        // Arrange
        Account account = TjsAccountWithBalance(100m);

        // Act
        Result result = account.Deposit(Tjs(50.50m));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Tjs(150.50m), account.Balance);
    }

    [Fact]
    public void Deposit_SeveralTimes_AccumulatesBalance()
    {
        Account account = TjsAccountWithBalance(0m);

        account.Deposit(Tjs(10m));
        account.Deposit(Tjs(20.25m));
        account.Deposit(Tjs(0.75m));

        Assert.Equal(Tjs(31m), account.Balance);
    }

    [Fact]
    public void Deposit_RaisesMoneyDepositedWithAmountAndBalanceAfter()
    {
        // Arrange
        Account account = TjsAccountWithBalance(100m);

        // Act
        account.Deposit(Tjs(50m));

        // Assert
        IDomainEvent domainEvent = Assert.Single(account.GetDomainEvents());
        MoneyDeposited deposited = Assert.IsType<MoneyDeposited>(domainEvent);
        Assert.Equal(account.Id, deposited.AccountId);
        Assert.Equal(Tjs(50m), deposited.Amount);
        Assert.Equal(Tjs(150m), deposited.BalanceAfter);
    }

    // ---------- Deposit: business errors ----------

    [Theory]
    [MemberData(nameof(NotPositiveAmounts))]
    public void Deposit_ZeroOrNegativeAmount_ReturnsAmountMustBePositiveAndChangesNothing(decimal amount)
    {
        // Arrange
        Account account = TjsAccountWithBalance(100m);

        // Act
        Result result = account.Deposit(Tjs(amount));

        // Assert
        Assert.Equal(AccountErrors.AmountMustBePositive, result.Error);
        AssertUnchanged(account, Tjs(100m));
    }

    [Fact]
    public void Deposit_DifferentCurrency_ReturnsCurrencyMismatchAndChangesNothing()
    {
        // Arrange
        Account account = TjsAccountWithBalance(100m);

        // Act
        Result result = account.Deposit(Usd(50m));

        // Assert
        Assert.Equal(AccountErrors.CurrencyMismatch, result.Error);
        AssertUnchanged(account, Tjs(100m));
    }

    [Fact]
    public void Deposit_NullAmount_ThrowsArgumentNullException()
    {
        Account account = TjsAccountWithBalance(100m);

        Assert.Throws<ArgumentNullException>(() => account.Deposit(null!));
    }

    // ---------- Withdraw: success ----------

    [Fact]
    public void Withdraw_LessThanBalance_DecreasesBalance()
    {
        // Arrange
        Account account = TjsAccountWithBalance(100m);

        // Act
        Result result = account.Withdraw(Tjs(30.25m));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Tjs(69.75m), account.Balance);
    }

    [Fact]
    public void Withdraw_ExactlyTheBalance_LeavesZero()
    {
        Account account = TjsAccountWithBalance(100m);

        Result result = account.Withdraw(Tjs(100m));

        Assert.True(result.IsSuccess);
        Assert.True(account.Balance.IsZero);
    }

    [Fact]
    public void Withdraw_RaisesMoneyWithdrawnWithAmountAndBalanceAfter()
    {
        // Arrange
        Account account = TjsAccountWithBalance(100m);

        // Act
        account.Withdraw(Tjs(40m));

        // Assert
        IDomainEvent domainEvent = Assert.Single(account.GetDomainEvents());
        MoneyWithdrawn withdrawn = Assert.IsType<MoneyWithdrawn>(domainEvent);
        Assert.Equal(account.Id, withdrawn.AccountId);
        Assert.Equal(Tjs(40m), withdrawn.Amount);
        Assert.Equal(Tjs(60m), withdrawn.BalanceAfter);
    }

    // ---------- Withdraw: business errors ----------

    [Fact]
    public void Withdraw_MoreThanBalance_ReturnsInsufficientFundsAndChangesNothing()
    {
        // Arrange
        Account account = TjsAccountWithBalance(100m);

        // Act
        Result result = account.Withdraw(Tjs(100.01m));

        // Assert
        Assert.Equal(AccountErrors.InsufficientFunds, result.Error);
        AssertUnchanged(account, Tjs(100m));
    }

    [Fact]
    public void Withdraw_FromNewAccount_ReturnsInsufficientFunds()
    {
        Account account = TjsAccountWithBalance(0m);

        Result result = account.Withdraw(Tjs(0.01m));

        Assert.Equal(AccountErrors.InsufficientFunds, result.Error);
    }

    [Theory]
    [MemberData(nameof(NotPositiveAmounts))]
    public void Withdraw_ZeroOrNegativeAmount_ReturnsAmountMustBePositiveAndChangesNothing(decimal amount)
    {
        // A negative withdrawal would secretly be a deposit, so it must be rejected.
        Account account = TjsAccountWithBalance(100m);

        Result result = account.Withdraw(Tjs(amount));

        Assert.Equal(AccountErrors.AmountMustBePositive, result.Error);
        AssertUnchanged(account, Tjs(100m));
    }

    [Fact]
    public void Withdraw_DifferentCurrency_ReturnsCurrencyMismatchInsteadOfThrowing()
    {
        // The currency check must come BEFORE the funds check:
        // comparing TJS with USD inside Money would throw InvalidOperationException.
        Account account = TjsAccountWithBalance(100m);

        Result result = account.Withdraw(Usd(50m));

        Assert.Equal(AccountErrors.CurrencyMismatch, result.Error);
        AssertUnchanged(account, Tjs(100m));
    }

    [Fact]
    public void Withdraw_ZeroFromEmptyAccount_ReportsAmountProblemFirst()
    {
        // Input is validated before checking funds.
        Account account = TjsAccountWithBalance(0m);

        Result result = account.Withdraw(Tjs(0m));

        Assert.Equal(AccountErrors.AmountMustBePositive, result.Error);
    }

    [Fact]
    public void Withdraw_NullAmount_ThrowsArgumentNullException()
    {
        Account account = TjsAccountWithBalance(100m);

        Assert.Throws<ArgumentNullException>(() => account.Withdraw(null!));
    }

    // ---------- Deposit + Withdraw together ----------

    [Fact]
    public void DepositThenWithdraw_BalanceReflectsBothOperations()
    {
        Account account = TjsAccountWithBalance(0m);

        account.Deposit(Tjs(500m));
        account.Withdraw(Tjs(120.50m));
        account.Deposit(Tjs(20.50m));

        Assert.Equal(Tjs(400m), account.Balance);
        Assert.Equal(3, account.GetDomainEvents().Count);
    }

    // ---------- Helpers ----------

    private static void AssertUnchanged(Account account, Money expectedBalance)
    {
        Assert.Equal(expectedBalance, account.Balance);
        Assert.Empty(account.GetDomainEvents());
    }
}
