using Angur.Domain.Abstractions;

namespace Angur.Domain.Customers;

public sealed record FullName
{
    private FullName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
    public string FirstName { get; }
    public string LastName { get; }

    public const int MaxPartLength = 100;

    public static Result<FullName> Create(string? firstName, string? lastName)
    {
        if(string.IsNullOrWhiteSpace(firstName))
        {
            return Result.Failure<FullName>(FullNameErrors.FirstNameRequired);
        }

        if(string.IsNullOrWhiteSpace(lastName))
        {
            return Result.Failure<FullName>(FullNameErrors.LastNameRequired);
        }

        string trimmedFirstName = firstName.Trim(), trimmedLastName = lastName.Trim();

        if(trimmedFirstName.Length > MaxPartLength || trimmedLastName.Length > MaxPartLength)
        {
            return Result.Failure<FullName>(FullNameErrors.TooLong);
        }

        return Result.Success(new FullName(trimmedFirstName, trimmedLastName));
    }

    public override string ToString() => $"{FirstName} {LastName}";

}