using Angur.Domain.Abstractions;
using Angur.Domain.Customers.Events;

namespace Angur.Domain.Customers;

public sealed class Customer : AggregateRoot<CustomerId>
{
    public const int MinimumAge = 18;

    private Customer(
        CustomerId id,
        FullName name,
        Email email,
        DateOnly dateOfBirth,
        DateTimeOffset registeredAt)
        : base(id)
    {
        Name = name;
        Email = email;
        DateOfBirth = dateOfBirth;
        RegisteredAt = registeredAt;
        KycStatus = KycStatus.Pending;
    }

    public FullName Name { get; private set; }
    public Email Email { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    public KycStatus KycStatus { get; private set; }
    public DateTimeOffset? KycDecidedAt { get; private set; }
    public string? KycRejectionReason { get; private set; }

    public bool IsKycVerified => KycStatus == KycStatus.Verified;
    public static Result<Customer> Register(
        FullName name,
        Email email,
        DateOnly dateOfBirth,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);

        DateOnly today = DateOnly.FromDateTime(now.UtcDateTime);
        DateOnly adultFrom = dateOfBirth.AddYears(MinimumAge);

        if (today < adultFrom)
        {
            return Result.Failure<Customer>(CustomerErrors.UnderAge);
        }

        Customer customer = new(CustomerId.New(), name, email, dateOfBirth, now);
        customer.Raise(new CustomerRegistered(customer.Id));

        return Result.Success(customer);
    }


    public Result VerifyKyc(DateTimeOffset now)
    {
        if (KycStatus != KycStatus.Pending)
        {
            return Result.Failure(CustomerErrors.KycAlreadyDecided);
        }

        KycStatus = KycStatus.Verified;
        KycDecidedAt = now;
        Raise(new CustomerKycVerified(Id));

        return Result.Success();
    }


    public Result RejectKyc(string? reason, DateTimeOffset now)
    {
        if (KycStatus != KycStatus.Pending)
        {
            return Result.Failure(CustomerErrors.KycAlreadyDecided);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(CustomerErrors.RejectionReasonRequired);
        }

        KycStatus = KycStatus.Rejected;
        KycDecidedAt = now;
        KycRejectionReason = reason.Trim();
        Raise(new CustomerKycRejected(Id, KycRejectionReason));

        return Result.Success();
    }
}
