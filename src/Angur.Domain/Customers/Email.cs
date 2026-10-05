using System.Text.RegularExpressions;
using Angur.Domain.Abstractions;

namespace Angur.Domain.Customers;

public sealed partial record Email
{
    private Email(string value)
    {
        Value = value;
    }
    public string Value { get; }

    public const int MaxPartLength = 254;

    public static Result<Email> Create(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(EmailErrors.EmailRequired);
        }

        string normalized = value.Trim().ToLowerInvariant();

        if(normalized.Length > MaxPartLength)
        {
            return Result.Failure<Email>(EmailErrors.TooLong);
        }

        if(!EmailRegex().IsMatch(normalized))
        {
            return Result.Failure<Email>(EmailErrors.InvalidFormat);
        }

        return Result.Success(new Email(normalized));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}