using Angur.Application.Accounts.FreezeAccount;
using Angur.Application.UnitTests.Fakes;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests.Accounts;

public class FreezeAccountCommandHandlerTests
{
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FreezeAccountCommandHandler _handler;

    public FreezeAccountCommandHandlerTests()
    {
        _handler = new FreezeAccountCommandHandler(_accounts, new FakeTimeProvider(Now), _unitOfWork);
    }

    [Fact]
    public async Task Handle_ActiveAccount_FreezesWithReasonAtCurrentTimeAndSavesOnce()
    {
        // Arrange
        Account account = ActiveAccount();
        _accounts.Seed(account);

        // Act
        Result result = await Handle(new FreezeAccountCommand(account.Id.Value, "Court order 15/2026"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Frozen, account.Status);
        Assert.Equal("Court order 15/2026", account.FreezeReason);
        Assert.Equal(Now, account.FrozenAt);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_UnknownAccount_ReturnsAccountNotFoundAndSavesNothing()
    {
        Result result = await Handle(new FreezeAccountCommand(Guid.NewGuid(), "Court order"));

        Assert.Equal(AccountErrors.NotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyReason_ReturnsFreezeReasonRequiredAndSavesNothing(string reason)
    {
        Account account = ActiveAccount();
        _accounts.Seed(account);

        Result result = await Handle(new FreezeAccountCommand(account.Id.Value, reason));

        Assert.Equal(AccountErrors.FreezeReasonRequired, result.Error);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_AlreadyFrozen_ReturnsAccountAlreadyFrozenAndKeepsOriginalFreeze()
    {
        Account account = FrozenAccount();
        _accounts.Seed(account);

        Result result = await Handle(new FreezeAccountCommand(account.Id.Value, "Another reason"));

        Assert.Equal(AccountErrors.AccountAlreadyFrozen, result.Error);
        Assert.Equal("Suspicious activity", account.FreezeReason);
        Assert.Equal(Earlier, account.FrozenAt);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_ClosedAccount_ReturnsAccountClosedAndSavesNothing()
    {
        Account account = ClosedAccount();
        _accounts.Seed(account);

        Result result = await Handle(new FreezeAccountCommand(account.Id.Value, "Court order"));

        Assert.Equal(AccountErrors.AccountClosed, result.Error);
        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    private Task<Result> Handle(FreezeAccountCommand command) =>
        _handler.HandleAsync(command, TestContext.Current.CancellationToken);
}
