using Angur.Domain.Abstractions;

namespace Angur.Domain.Customers;

public static class EmailErrors
{
    public static readonly DomainError EmailRequired = new("Email.EmailRequired", "Email is required to fill.", ErrorType.Validation);
    public static readonly DomainError TooLong = new("Email.TooLong", "The email is too long", ErrorType.Validation);
    public static readonly DomainError InvalidFormat = new("Email.InvalidFormat", "The format of email is invalid.", ErrorType.Validation);
}
