using Angur.Domain.Abstractions;
using Angur.Domain.Customers;
using Angur.Domain.Customers.Events;

namespace Angur.Domain.UnitTests.Customers;

public class CustomerTests
{
    // A fixed "now" makes every age calculation predictable, whatever day the tests run.
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DecisionTime = Now.AddDays(2);

    private static readonly FullName ValidName = FullName.Create("Ali", "Karimov").Value;
    private static readonly Email ValidEmail = Email.Create("ali@mail.tj").Value;
    private static readonly DateOnly AdultBirthDate = new(1990, 6, 1);

    // ---------- Register: success ----------

    [Fact]
    public void Register_Adult_ReturnsCustomerWithGivenData()
    {
        // Act
        Result<Customer> result = Customer.Register(ValidName, ValidEmail, AdultBirthDate, Now);

        // Assert
        Assert.True(result.IsSuccess);
        Customer customer = result.Value;
        Assert.Equal(ValidName, customer.Name);
        Assert.Equal(ValidEmail, customer.Email);
        Assert.Equal(AdultBirthDate, customer.DateOfBirth);
        Assert.Equal(Now, customer.RegisteredAt);
    }

    [Fact]
    public void Register_NewCustomer_StartsWithPendingKycAndNoDecision()
    {
        // Act
        Customer customer = RegisterAdult();

        // Assert
        Assert.Equal(KycStatus.Pending, customer.KycStatus);
        Assert.False(customer.IsKycVerified);
        Assert.Null(customer.KycDecidedAt);
        Assert.Null(customer.KycRejectionReason);
    }

    [Fact]
    public void Register_RaisesCustomerRegisteredWithCustomerId()
    {
        // Act
        Customer customer = RegisterAdult();

        // Assert
        IDomainEvent domainEvent = Assert.Single(customer.GetDomainEvents());
        CustomerRegistered registered = Assert.IsType<CustomerRegistered>(domainEvent);
        Assert.Equal(customer.Id, registered.CustomerId);
    }

    [Fact]
    public void Register_TwoCustomers_GetDifferentIds()
    {
        Customer first = RegisterAdult();
        Customer second = RegisterAdult();

        Assert.NotEqual(first.Id, second.Id);
    }

    // ---------- Register: age rule ----------

    [Fact]
    public void Register_Turns18Today_Succeeds()
    {
        // Now = 2026-03-15, so born 2008-03-15 turns exactly 18 today.
        DateOnly birthDate = new(2008, 3, 15);

        Result<Customer> result = Customer.Register(ValidName, ValidEmail, birthDate, Now);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(2008, 3, 16)]   // turns 18 tomorrow
    [InlineData(2008, 12, 31)]  // turns 18 later this year (catches "year minus year" bugs)
    [InlineData(2015, 1, 1)]    // child
    [InlineData(2030, 1, 1)]    // birth date in the future
    public void Register_YoungerThan18_ReturnsUnderAge(int year, int month, int day)
    {
        // Act
        Result<Customer> result = Customer.Register(ValidName, ValidEmail, new DateOnly(year, month, day), Now);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.UnderAge, result.Error);
    }

    [Fact]
    public void Register_BornOnFeb29_IsAdultOnFeb28OfNonLeapYear()
    {
        // 2026 is not a leap year: DateOnly.AddYears moves Feb 29 to Feb 28.
        DateOnly leapDayBirth = new(2008, 2, 29);
        DateTimeOffset feb28 = new(2026, 2, 28, 10, 0, 0, TimeSpan.Zero);

        Result<Customer> result = Customer.Register(ValidName, ValidEmail, leapDayBirth, feb28);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Register_BornOnFeb29_IsUnderAgeOnFeb27()
    {
        DateOnly leapDayBirth = new(2008, 2, 29);
        DateTimeOffset feb27 = new(2026, 2, 27, 10, 0, 0, TimeSpan.Zero);

        Result<Customer> result = Customer.Register(ValidName, ValidEmail, leapDayBirth, feb27);

        Assert.Equal(CustomerErrors.UnderAge, result.Error);
    }

    [Fact]
    public void Register_AgeIsCheckedAgainstUtcDate()
    {
        // 01:00 on March 15 in Dushanbe (UTC+5) is still March 14 in UTC,
        // so someone born 2008-03-15 is not 18 yet by the UTC date.
        DateTimeOffset earlyMorningInDushanbe = new(2026, 3, 15, 1, 0, 0, TimeSpan.FromHours(5));

        Result<Customer> result = Customer.Register(ValidName, ValidEmail, new DateOnly(2008, 3, 15), earlyMorningInDushanbe);

        Assert.Equal(CustomerErrors.UnderAge, result.Error);
    }

    // ---------- Register: programmer errors ----------

    [Fact]
    public void Register_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Customer.Register(null!, ValidEmail, AdultBirthDate, Now));
    }

    [Fact]
    public void Register_NullEmail_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Customer.Register(ValidName, null!, AdultBirthDate, Now));
    }

    // ---------- VerifyKyc ----------

    [Fact]
    public void VerifyKyc_WhenPending_VerifiesAndRecordsDecisionTime()
    {
        // Arrange
        Customer customer = RegisterAdult();

        // Act
        Result result = customer.VerifyKyc(DecisionTime);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(KycStatus.Verified, customer.KycStatus);
        Assert.True(customer.IsKycVerified);
        Assert.Equal(DecisionTime, customer.KycDecidedAt);
        Assert.Null(customer.KycRejectionReason);
    }

    [Fact]
    public void VerifyKyc_WhenPending_RaisesCustomerKycVerified()
    {
        // Arrange
        Customer customer = RegisterAdult();
        customer.ClearDomainEvents();

        // Act
        customer.VerifyKyc(DecisionTime);

        // Assert
        IDomainEvent domainEvent = Assert.Single(customer.GetDomainEvents());
        CustomerKycVerified verified = Assert.IsType<CustomerKycVerified>(domainEvent);
        Assert.Equal(customer.Id, verified.CustomerId);
    }

    // ---------- RejectKyc ----------

    [Fact]
    public void RejectKyc_WhenPending_RejectsWithTrimmedReasonAndDecisionTime()
    {
        // Arrange
        Customer customer = RegisterAdult();

        // Act
        Result result = customer.RejectKyc("  Forged passport  ", DecisionTime);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(KycStatus.Rejected, customer.KycStatus);
        Assert.False(customer.IsKycVerified);
        Assert.Equal(DecisionTime, customer.KycDecidedAt);
        Assert.Equal("Forged passport", customer.KycRejectionReason);
    }

    [Fact]
    public void RejectKyc_WhenPending_RaisesCustomerKycRejectedWithTrimmedReason()
    {
        // Arrange
        Customer customer = RegisterAdult();
        customer.ClearDomainEvents();

        // Act
        customer.RejectKyc("  Forged passport  ", DecisionTime);

        // Assert
        IDomainEvent domainEvent = Assert.Single(customer.GetDomainEvents());
        CustomerKycRejected rejected = Assert.IsType<CustomerKycRejected>(domainEvent);
        Assert.Equal(customer.Id, rejected.CustomerId);
        Assert.Equal("Forged passport", rejected.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectKyc_WithoutReason_ReturnsReasonRequiredAndChangesNothing(string? reason)
    {
        // Arrange
        Customer customer = RegisterAdult();
        customer.ClearDomainEvents();

        // Act
        Result result = customer.RejectKyc(reason, DecisionTime);

        // Assert
        Assert.Equal(CustomerErrors.RejectionReasonRequired, result.Error);
        AssertStillPending(customer);
    }

    // ---------- Decisions are final ----------

    [Theory]
    [InlineData(true, true)]    // verified  -> verify again
    [InlineData(true, false)]   // verified  -> reject
    [InlineData(false, true)]   // rejected  -> verify
    [InlineData(false, false)]  // rejected  -> reject again
    public void KycDecision_WhenAlreadyDecided_ReturnsKycAlreadyDecidedAndChangesNothing(
        bool firstVerify,
        bool thenVerify)
    {
        // Arrange
        Customer customer = RegisterAdult();
        Decide(customer, firstVerify, DecisionTime);
        customer.ClearDomainEvents();

        KycStatus statusBefore = customer.KycStatus;
        DateTimeOffset? decidedAtBefore = customer.KycDecidedAt;
        string? reasonBefore = customer.KycRejectionReason;

        // Act
        Result result = Decide(customer, thenVerify, DecisionTime.AddDays(1));

        // Assert
        Assert.Equal(CustomerErrors.KycAlreadyDecided, result.Error);
        Assert.Equal(statusBefore, customer.KycStatus);
        Assert.Equal(decidedAtBefore, customer.KycDecidedAt);
        Assert.Equal(reasonBefore, customer.KycRejectionReason);
        Assert.Empty(customer.GetDomainEvents());
    }

    [Fact]
    public void RejectKyc_AlreadyDecidedAndNoReason_ReportsAlreadyDecided()
    {
        // The state problem is more important than the missing reason.
        Customer customer = RegisterAdult();
        customer.VerifyKyc(DecisionTime);

        Result result = customer.RejectKyc(null, DecisionTime);

        Assert.Equal(CustomerErrors.KycAlreadyDecided, result.Error);
    }

    // ---------- Helpers ----------

    private static Customer RegisterAdult() =>
        Customer.Register(ValidName, ValidEmail, AdultBirthDate, Now).Value;

    private static Result Decide(Customer customer, bool verify, DateTimeOffset when) =>
        verify
            ? customer.VerifyKyc(when)
            : customer.RejectKyc("Documents are not valid", when);

    private static void AssertStillPending(Customer customer)
    {
        Assert.Equal(KycStatus.Pending, customer.KycStatus);
        Assert.Null(customer.KycDecidedAt);
        Assert.Null(customer.KycRejectionReason);
        Assert.Empty(customer.GetDomainEvents());
    }
}
