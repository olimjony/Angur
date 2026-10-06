using Angur.Application.Accounts.Withdraw;
using Angur.Application.UnitTests.Fakes;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests.Accounts;

public class WithdrawCommandHandlerTests
{
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly WithdrawCommandHandler _handler;

    public WithdrawCommandHandlerTests()
    {
        _handler = new WithdrawCommandHandler(_accounts, _unitOfWork);
    }

    // ---------- Success ----------

    [Fact]
    public async Task Handle_EnoughFunds_DecreasesBalanceAndSavesOnce()
    {
        // Arrange
        Account account = Seed(ActiveAccount(balance: 100m));

        // Act
        Result result = await Handle(new WithdrawCommand(account.Id.Value, 30.50m, "TJS"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Tjs(69.50m), account.Balance);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_WholeBalance_LeavesZero()
    {
        Account account = Seed(ActiveAccount(balance: 100m));

        Result result = await Handle(new WithdrawCommand(account.Id.Value, 100m, "TJS"));

        Assert.True(result.IsSuccess);
        Assert.True(account.Balance.IsZero);
    }

    // ---------- Bad input ----------

    [Fact]
    public async Task Handle_UnsupportedCurrency_ReturnsCurrencyError()
    {
        Account account = Seed(ActiveAccount(balance: 100m));

        Result result = await Handle(new WithdrawCommand(account.Id.Value, 10m, "XYZ"));

        Assert.Equal(CurrencyErrors.Unsupported, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(100m));
    }

    [Fact]
    public async Task Handle_TooManyDecimalPlaces_ReturnsInvalidScale()
    {
        Account account = Seed(ActiveAccount(balance: 100m));

        Result result = await Handle(new WithdrawCommand(account.Id.Value, 0.001m, "TJS"));

        Assert.Equal(MoneyErrors.InvalidScale, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(100m));
    }

    [Fact]
    public async Task Handle_UnknownAccount_ReturnsAccountNotFoundAndSavesNothing()
    {
        Result result = await Handle(new WithdrawCommand(Guid.NewGuid(), 10m, "TJS"));

        Assert.Equal(AccountErrors.NotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    // ---------- Domain says no ----------

    [Fact]
    public async Task Handle_MoreThanBalance_ReturnsInsufficientFunds()
    {
        Account account = Seed(ActiveAccount(balance: 100m));

        Result result = await Handle(new WithdrawCommand(account.Id.Value, 100.01m, "TJS"));

        Assert.Equal(AccountErrors.InsufficientFunds, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(100m));
    }

    [Fact]
    public async Task Handle_CurrencyDifferentFromAccount_ReturnsCurrencyMismatch()
    {
        Account account = Seed(ActiveAccount(balance: 100m));

        Result result = await Handle(new WithdrawCommand(account.Id.Value, 10m, "USD"));

        Assert.Equal(AccountErrors.CurrencyMismatch, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(100m));
    }

    [Fact]
    public async Task Handle_FrozenAccount_ReturnsAccountFrozen()
    {
        Account account = Seed(FrozenAccount(balance: 100m));

        Result result = await Handle(new WithdrawCommand(account.Id.Value, 10m, "TJS"));

        Assert.Equal(AccountErrors.AccountFrozen, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(100m));
    }

    [Fact]
    public async Task Handle_ClosedAccount_ReturnsAccountClosed()
    {
        Account account = Seed(ClosedAccount());

        Result result = await Handle(new WithdrawCommand(account.Id.Value, 10m, "TJS"));

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(0m));
    }

    // ---------- Helpers ----------

    private Account Seed(Account account)
    {
        _accounts.Seed(account);
        return account;
    }

    private Task<Result> Handle(WithdrawCommand command) =>
        _handler.HandleAsync(command, TestContext.Current.CancellationToken);

    private void AssertUnchangedAndNotSaved(Account account, Money expectedBalance)
    {
        Assert.Equal(expectedBalance, account.Balance);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}
