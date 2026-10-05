using Angur.Domain.Abstractions;

namespace Angur.Domain.Customers;

public static class CustomerErrors
{
    public static readonly DomainError UnderAge = new("Customer.Underage", "Customer must be at least 18 years old.", ErrorType.Validation);
    public static readonly DomainError KycAlreadyDecided = new("Customer.KycAlreadyDecided", "Kyc was already processed.", ErrorType.Conflict);
    public static readonly DomainError RejectionReasonRequired = new("Customer.RejectionReasonRequired", "Rejection reason of Kyc is required..", ErrorType.Validation);
}