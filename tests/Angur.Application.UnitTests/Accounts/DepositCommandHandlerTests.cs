using Angur.Application.Accounts.Deposit;
using Angur.Application.UnitTests.Fakes;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests.Accounts;

public class DepositCommandHandlerTests
{
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DepositCommandHandler _handler;

    public DepositCommandHandlerTests()
    {
        _handler = new DepositCommandHandler(_accounts, _unitOfWork);
    }

    // ---------- Success ----------

    [Fact]
    public async Task Handle_ActiveAccount_IncreasesBalanceAndSavesOnce()
    {
        // Arrange
        Account account = Seed(ActiveAccount(balance: 100m));

        // Act
        Result result = await Handle(new DepositCommand(account.Id.Value, 50.25m, "TJS"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Tjs(150.25m), account.Balance);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_FrozenAccount_StillAcceptsMoney()
    {
        // Freezing blocks debits only (domain rule); the handler must not add its own restriction.
        Account account = Seed(FrozenAccount(balance: 100m));

        Result result = await Handle(new DepositCommand(account.Id.Value, 50m, "TJS"));

        Assert.True(result.IsSuccess);
        Assert.Equal(Tjs(150m), account.Balance);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    // ---------- Bad input: rejected before loading the account ----------

    [Fact]
    public async Task Handle_UnsupportedCurrency_ReturnsCurrencyError()
    {
        Account account = Seed(ActiveAccount(balance: 100m));

        Result result = await Handle(new DepositCommand(account.Id.Value, 50m, "XYZ"));

        Assert.Equal(CurrencyErrors.Unsupported, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(100m));
    }

    [Fact]
    public async Task Handle_TooManyDecimalPlaces_ReturnsInvalidScale()
    {
        Account account = Seed(ActiveAccount(balance: 100m));

        Result result = await Handle(new DepositCommand(account.Id.Value, 10.001m, "TJS"));

        Assert.Equal(MoneyErrors.InvalidScale, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(100m));
    }

    [Fact]
    public async Task Handle_UnknownAccount_ReturnsAccountNotFoundAndSavesNothing()
    {
        Result result = await Handle(new DepositCommand(Guid.NewGuid(), 50m, "TJS"));

        Assert.Equal(AccountErrors.NotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    // ---------- Domain says no ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Handle_ZeroOrNegativeAmount_ReturnsAmountMustBePositive(decimal amount)
    {
        Account account = Seed(ActiveAccount(balance: 100m));

        Result result = await Handle(new DepositCommand(account.Id.Value, amount, "TJS"));

        Assert.Equal(AccountErrors.AmountMustBePositive, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(100m));
    }

    [Fact]
    public async Task Handle_CurrencyDifferentFromAccount_ReturnsCurrencyMismatch()
    {
        // "USD" is a valid currency, but the account is in TJS.
        Account account = Seed(ActiveAccount(balance: 100m));

        Result result = await Handle(new DepositCommand(account.Id.Value, 50m, "USD"));

        Assert.Equal(AccountErrors.CurrencyMismatch, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(100m));
    }

    [Fact]
    public async Task Handle_ClosedAccount_ReturnsAccountClosed()
    {
        Account account = Seed(ClosedAccount());

        Result result = await Handle(new DepositCommand(account.Id.Value, 50m, "TJS"));

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        AssertUnchangedAndNotSaved(account, Tjs(0m));
    }

    // ---------- Helpers ----------

    private Account Seed(Account account)
    {
        _accounts.Seed(account);
        return account;
    }

    private Task<Result> Handle(DepositCommand command) =>
        _handler.HandleAsync(command, TestContext.Current.CancellationToken);

    private void AssertUnchangedAndNotSaved(Account account, Money expectedBalance)
    {
        Assert.Equal(expectedBalance, account.Balance);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}
