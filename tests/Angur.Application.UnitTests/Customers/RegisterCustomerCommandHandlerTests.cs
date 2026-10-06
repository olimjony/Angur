using Angur.Application.Customers.RegisterCustomer;
using Angur.Application.UnitTests.Fakes;
using Angur.Domain.Abstractions;
using Angur.Domain.Customers;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests.Customers;

public class RegisterCustomerCommandHandlerTests
{
    // xUnit creates a NEW instance of this class for every test,
    // so every test gets its own empty fakes. No state leaks between tests.
    private readonly FakeCustomerRepository _customers = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly RegisterCustomerCommandHandler _handler;

    public RegisterCustomerCommandHandlerTests()
    {
        _handler = new RegisterCustomerCommandHandler(_customers, _unitOfWork, new FakeTimeProvider(Now));
    }

    public static TheoryData<string, string, string, DomainError> InvalidInput => new()
    {
        { "", "Karimov", "ali@mail.tj", FullNameErrors.FirstNameRequired },
        { "Ali", "   ", "ali@mail.tj", FullNameErrors.LastNameRequired },
        { "Ali", "Karimov", "", EmailErrors.EmailRequired },
        { "Ali", "Karimov", "not-an-email", EmailErrors.InvalidFormat },
    };

    // ---------- Success ----------

    [Fact]
    public async Task Handle_ValidCommand_AddsCustomerSavesOnceAndReturnsItsId()
    {
        // Arrange
        RegisterCustomerCommand command = new("Ali", "Karimov", "ali@mail.tj", new DateOnly(1990, 6, 1));

        // Act
        Result<Guid> result = await Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Customer added = Assert.Single(_customers.Added);
        Assert.Equal(added.Id.Value, result.Value);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Handle_ValidCommand_PassesNormalizedDataAndCurrentTimeToTheDomain()
    {
        RegisterCustomerCommand command = new("  Ali ", " Karimov ", "  Ali@Mail.TJ ", new DateOnly(1990, 6, 1));

        await Handle(command);

        Customer added = Assert.Single(_customers.Added);
        Assert.Equal("Ali Karimov", added.Name.ToString());
        Assert.Equal("ali@mail.tj", added.Email.Value);
        Assert.Equal(new DateOnly(1990, 6, 1), added.DateOfBirth);
        Assert.Equal(Now, added.RegisteredAt);
        Assert.Equal(KycStatus.Pending, added.KycStatus);
    }

    [Fact]
    public async Task Handle_TurnsEighteenExactlyToday_Succeeds()
    {
        // "Today" is the FAKE clock's date, not the real one.
        DateOnly today = DateOnly.FromDateTime(Now.UtcDateTime);
        RegisterCustomerCommand command = new("Ali", "Karimov", "ali@mail.tj", today.AddYears(-18));

        Result<Guid> result = await Handle(command);

        Assert.True(result.IsSuccess);
    }

    // ---------- Failures: nothing is added, nothing is saved ----------

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public async Task Handle_InvalidInput_ReturnsValidationErrorAndSavesNothing(
        string firstName, string lastName, string email, DomainError expected)
    {
        // Arrange
        RegisterCustomerCommand command = new(firstName, lastName, email, new DateOnly(1990, 6, 1));

        // Act
        Result<Guid> result = await Handle(command);

        // Assert
        Assert.Equal(expected, result.Error);
        AssertNothingSaved();
    }

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_ReturnsConflictAndSavesNothing()
    {
        // Arrange: the same address, written differently, is already taken.
        _customers.Seed(PendingCustomer("ali@mail.tj"));
        RegisterCustomerCommand command = new("Ali", "Rahimov", "  ALI@Mail.tj ", new DateOnly(1995, 1, 1));

        // Act
        Result<Guid> result = await Handle(command);

        // Assert
        Assert.Equal(CustomerErrors.EmailAlreadyRegistered, result.Error);
        AssertNothingSaved();
    }

    [Fact]
    public async Task Handle_UnderAgeAccordingToTheClock_ReturnsUnderAgeAndSavesNothing()
    {
        // Turns 18 tomorrow by the fake clock. If the handler used the REAL clock
        // (DateTimeOffset.UtcNow, months later), this person would already be 18 and the test would fail.
        DateOnly today = DateOnly.FromDateTime(Now.UtcDateTime);
        RegisterCustomerCommand command = new("Ali", "Karimov", "ali@mail.tj", today.AddYears(-18).AddDays(1));

        Result<Guid> result = await Handle(command);

        Assert.Equal(CustomerErrors.UnderAge, result.Error);
        AssertNothingSaved();
    }

    // ---------- Helpers ----------

    private Task<Result<Guid>> Handle(RegisterCustomerCommand command) =>
        _handler.HandleAsync(command, TestContext.Current.CancellationToken);

    private void AssertNothingSaved()
    {
        Assert.Empty(_customers.Added);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}
