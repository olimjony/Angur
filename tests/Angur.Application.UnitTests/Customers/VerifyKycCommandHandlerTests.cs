using Angur.Application.Customers.VerifyKyc;
using Angur.Application.UnitTests.Fakes;
using Angur.Domain.Abstractions;
using Angur.Domain.Customers;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests.Customers;

public class VerifyKycCommandHandlerTests
{
    private readonly FakeCustomerRepository _customers = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly VerifyKycCommandHandler _handler;

    public VerifyKycCommandHandlerTests()
    {
        _handler = new VerifyKycCommandHandler(_customers, new FakeTimeProvider(Now), _unitOfWork);
    }

    [Fact]
    public async Task Handle_PendingCustomer_VerifiesAtCurrentTimeAndSavesOnce()
    {
        // Arrange
        Customer customer = PendingCustomer();
        _customers.Seed(customer);

        // Act
        Result result = await Handle(new VerifyKycCommand(customer.Id.Value));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(KycStatus.Verified, customer.KycStatus);
        Assert.Equal(Now, customer.KycDecidedAt);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsNotFoundAndSavesNothing()
    {
        Result result = await Handle(new VerifyKycCommand(Guid.NewGuid()));

        Assert.Equal(CustomerErrors.NotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_AlreadyVerified_ReturnsKycAlreadyDecidedAndSavesNothing()
    {
        Customer customer = VerifiedCustomer();
        _customers.Seed(customer);

        Result result = await Handle(new VerifyKycCommand(customer.Id.Value));

        Assert.Equal(CustomerErrors.KycAlreadyDecided, result.Error);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_AlreadyRejected_ReturnsKycAlreadyDecidedAndKeepsRejection()
    {
        Customer customer = RejectedCustomer();
        _customers.Seed(customer);

        Result result = await Handle(new VerifyKycCommand(customer.Id.Value));

        Assert.Equal(CustomerErrors.KycAlreadyDecided, result.Error);
        Assert.Equal(KycStatus.Rejected, customer.KycStatus);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    private Task<Result> Handle(VerifyKycCommand command) =>
        _handler.HandleAsync(command, TestContext.Current.CancellationToken);
}
