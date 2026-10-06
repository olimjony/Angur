using Angur.Application.Accounts.UnfreezeAccount;
using Angur.Application.UnitTests.Fakes;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests.Accounts;

public class UnfreezeAccountCommandHandlerTests
{
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly UnfreezeAccountCommandHandler _handler;

    public UnfreezeAccountCommandHandlerTests()
    {
        _handler = new UnfreezeAccountCommandHandler(_accounts, _unitOfWork);
    }

    [Fact]
    public async Task Handle_FrozenAccount_BecomesActiveAndSavesOnce()
    {
        // Arrange
        Account account = FrozenAccount();
        _accounts.Seed(account);

        // Act
        Result result = await Handle(new UnfreezeAccountCommand(account.Id.Value));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Null(account.FreezeReason);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_UnknownAccount_ReturnsAccountNotFoundAndSavesNothing()
    {
        Result result = await Handle(new UnfreezeAccountCommand(Guid.NewGuid()));

        Assert.Equal(AccountErrors.NotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_ActiveAccount_ReturnsAccountNotFrozenAndSavesNothing()
    {
        Account account = ActiveAccount();
        _accounts.Seed(account);

        Result result = await Handle(new UnfreezeAccountCommand(account.Id.Value));

        Assert.Equal(AccountErrors.AccountNotFrozen, result.Error);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_ClosedAccount_ReturnsAccountClosedAndSavesNothing()
    {
        Account account = ClosedAccount();
        _accounts.Seed(account);

        Result result = await Handle(new UnfreezeAccountCommand(account.Id.Value));

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    private Task<Result> Handle(UnfreezeAccountCommand command) =>
        _handler.HandleAsync(command, TestContext.Current.CancellationToken);
}
