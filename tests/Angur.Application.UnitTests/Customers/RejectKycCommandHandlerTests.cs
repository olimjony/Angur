using Angur.Application.Customers.RejectKyc;
using Angur.Application.UnitTests.Fakes;
using Angur.Domain.Abstractions;
using Angur.Domain.Customers;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests.Customers;

public class RejectKycCommandHandlerTests
{
    private readonly FakeCustomerRepository _customers = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly RejectKycCommandHandler _handler;

    public RejectKycCommandHandlerTests()
    {
        _handler = new RejectKycCommandHandler(_customers, new FakeTimeProvider(Now), _unitOfWork);
    }

    [Fact]
    public async Task Handle_PendingCustomer_RejectsWithReasonAtCurrentTimeAndSavesOnce()
    {
        // Arrange
        Customer customer = PendingCustomer();
        _customers.Seed(customer);

        // Act
        Result result = await Handle(new RejectKycCommand(customer.Id.Value, "  Passport expired  "));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(KycStatus.Rejected, customer.KycStatus);
        Assert.Equal("Passport expired", customer.KycRejectionReason);
        Assert.Equal(Now, customer.KycDecidedAt);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsNotFoundAndSavesNothing()
    {
        Result result = await Handle(new RejectKycCommand(Guid.NewGuid(), "Passport expired"));

        Assert.Equal(CustomerErrors.NotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyReason_ReturnsRejectionReasonRequiredAndSavesNothing(string reason)
    {
        // The handler does not check the reason itself: the domain does, and the handler passes the error on.
        Customer customer = PendingCustomer();
        _customers.Seed(customer);

        Result result = await Handle(new RejectKycCommand(customer.Id.Value, reason));

        Assert.Equal(CustomerErrors.RejectionReasonRequired, result.Error);
        Assert.Equal(KycStatus.Pending, customer.KycStatus);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_AlreadyVerified_ReturnsKycAlreadyDecidedAndSavesNothing()
    {
        Customer customer = VerifiedCustomer();
        _customers.Seed(customer);

        Result result = await Handle(new RejectKycCommand(customer.Id.Value, "Changed our mind"));

        Assert.Equal(CustomerErrors.KycAlreadyDecided, result.Error);
        Assert.Equal(KycStatus.Verified, customer.KycStatus);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    private Task<Result> Handle(RejectKycCommand command) =>
        _handler.HandleAsync(command, TestContext.Current.CancellationToken);
}
