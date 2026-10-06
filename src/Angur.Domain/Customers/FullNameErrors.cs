using Angur.Domain.Abstractions;

namespace Angur.Domain.Customers;

public static class FullNameErrors
{
    public static readonly DomainError FirstNameRequired = new("FullName.FirstNameRequired", "FirstName is required to fill.", ErrorType.Validation);
    public static readonly DomainError LastNameRequired = new("FullName.LastNameRequired", "LastName is required to fill.", ErrorType.Validation);
    public static readonly DomainError TooLong = new("FullName.TooLong", "The name is too long", ErrorType.Validation);
}
