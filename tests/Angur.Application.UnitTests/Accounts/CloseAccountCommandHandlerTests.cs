using Angur.Application.Accounts.CloseAccount;
using Angur.Application.UnitTests.Fakes;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests.Accounts;

public class CloseAccountCommandHandlerTests
{
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CloseAccountCommandHandler _handler;

    public CloseAccountCommandHandlerTests()
    {
        _handler = new CloseAccountCommandHandler(_accounts, new FakeTimeProvider(Now), _unitOfWork);
    }

    [Fact]
    public async Task Handle_EmptyActiveAccount_ClosesAtCurrentTimeAndSavesOnce()
    {
        // Arrange
        Account account = ActiveAccount(balance: 0m);
        _accounts.Seed(account);

        // Act
        Result result = await Handle(new CloseAccountCommand(account.Id.Value));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.Equal(Now, account.ClosedAt);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_UnknownAccount_ReturnsAccountNotFoundAndSavesNothing()
    {
        Result result = await Handle(new CloseAccountCommand(Guid.NewGuid()));

        Assert.Equal(AccountErrors.NotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_AccountWithFunds_ReturnsNonZeroBalanceAndSavesNothing()
    {
        Account account = ActiveAccount(balance: 0.01m);
        _accounts.Seed(account);

        Result result = await Handle(new CloseAccountCommand(account.Id.Value));

        Assert.Equal(AccountErrors.NonZeroBalance, result.Error);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_FrozenAccount_ReturnsAccountFrozenAndSavesNothing()
    {
        Account account = FrozenAccount(balance: 0m);
        _accounts.Seed(account);

        Result result = await Handle(new CloseAccountCommand(account.Id.Value));

        Assert.Equal(AccountErrors.AccountFrozen, result.Error);
        Assert.Equal(AccountStatus.Frozen, account.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_AlreadyClosed_ReturnsAccountClosedAndKeepsOriginalCloseTime()
    {
        Account account = ClosedAccount();
        _accounts.Seed(account);

        Result result = await Handle(new CloseAccountCommand(account.Id.Value));

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        Assert.Equal(Earlier, account.ClosedAt);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    private Task<Result> Handle(CloseAccountCommand command) =>
        _handler.HandleAsync(command, TestContext.Current.CancellationToken);
}
