using Angur.Application.Accounts.OpenAccount;
using Angur.Application.UnitTests.Fakes;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;
using Angur.Domain.Customers;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests.Accounts;

public class OpenAccountCommandHandlerTests
{
    private readonly FakeCustomerRepository _customers = new();
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeAccountNumberGenerator _numbers = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly OpenAccountCommandHandler _handler;

    public OpenAccountCommandHandlerTests()
    {
        _handler = new OpenAccountCommandHandler(
            _customers, _accounts, _numbers, _unitOfWork, new FakeTimeProvider(Now));
    }

    // ---------- Success ----------

    [Fact]
    public async Task Handle_VerifiedCustomer_AddsAccountSavesOnceAndReturnsItsId()
    {
        // Arrange
        Customer owner = VerifiedCustomer();
        _customers.Seed(owner);

        // Act
        Result<Guid> result = await Handle(new OpenAccountCommand(owner.Id.Value, "USD"));

        // Assert
        Assert.True(result.IsSuccess);
        Account added = Assert.Single(_accounts.Added);
        Assert.Equal(added.Id.Value, result.Value);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_VerifiedCustomer_OpensAccountWithGeneratedNumberCurrencyAndCurrentTime()
    {
        Customer owner = VerifiedCustomer();
        _customers.Seed(owner);

        await Handle(new OpenAccountCommand(owner.Id.Value, "USD"));

        Account added = Assert.Single(_accounts.Added);
        Assert.Equal(owner.Id, added.CustomerId);
        Assert.Equal(FakeAccountNumberGenerator.Number, added.Number);
        Assert.Same(Currency.USD, added.Currency);
        Assert.True(added.Balance.IsZero);
        Assert.Equal(Now, added.OpenedAt);
    }

    [Fact]
    public async Task Handle_VerifiedCustomer_AsksGeneratorForANumberInTheAccountCurrency()
    {
        Customer owner = VerifiedCustomer();
        _customers.Seed(owner);

        await Handle(new OpenAccountCommand(owner.Id.Value, "EUR"));

        Currency requested = Assert.Single(_numbers.Requests);
        Assert.Same(Currency.EUR, requested);
    }

    [Fact]
    public async Task Handle_LowercaseCurrencyCode_IsAccepted()
    {
        Customer owner = VerifiedCustomer();
        _customers.Seed(owner);

        Result<Guid> result = await Handle(new OpenAccountCommand(owner.Id.Value, "tjs"));

        Assert.True(result.IsSuccess);
        Assert.Same(Currency.TJS, Assert.Single(_accounts.Added).Currency);
    }

    // ---------- Failures ----------

    [Fact]
    public async Task Handle_UnsupportedCurrency_ReturnsErrorWithoutTouchingTheDatabaseOrGenerator()
    {
        // Input is checked first: a bad currency must not cost a DB query or burn an account number.
        Customer owner = VerifiedCustomer();
        _customers.Seed(owner);

        Result<Guid> result = await Handle(new OpenAccountCommand(owner.Id.Value, "XYZ"));

        Assert.Equal(CurrencyErrors.Unsupported, result.Error);
        Assert.Empty(_numbers.Requests);
        AssertNothingSaved();
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsCustomerNotFoundAndDoesNotGenerateNumber()
    {
        Result<Guid> result = await Handle(new OpenAccountCommand(Guid.NewGuid(), "TJS"));

        Assert.Equal(CustomerErrors.NotFound, result.Error);
        Assert.Empty(_numbers.Requests);
        AssertNothingSaved();
    }

    [Fact]
    public async Task Handle_CustomerWithPendingKyc_ReturnsCustomerNotVerifiedAndSavesNothing()
    {
        Customer owner = PendingCustomer();
        _customers.Seed(owner);

        Result<Guid> result = await Handle(new OpenAccountCommand(owner.Id.Value, "TJS"));

        Assert.Equal(AccountErrors.CustomerNotVerified, result.Error);
        AssertNothingSaved();
    }

    [Fact]
    public async Task Handle_CustomerWithRejectedKyc_ReturnsCustomerNotVerifiedAndSavesNothing()
    {
        Customer owner = RejectedCustomer();
        _customers.Seed(owner);

        Result<Guid> result = await Handle(new OpenAccountCommand(owner.Id.Value, "TJS"));

        Assert.Equal(AccountErrors.CustomerNotVerified, result.Error);
        AssertNothingSaved();
    }

    // ---------- Helpers ----------

    private Task<Result<Guid>> Handle(OpenAccountCommand command) =>
        _handler.HandleAsync(command, TestContext.Current.CancellationToken);

    private void AssertNothingSaved()
    {
        Assert.Empty(_accounts.Added);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}
