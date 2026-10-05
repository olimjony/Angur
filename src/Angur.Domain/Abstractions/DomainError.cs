namespace Angur.Domain.Abstractions;

public sealed record DomainError(string Code, string Description, ErrorType ErrorType)
{
    public static readonly DomainError None = new(string.Empty, string.Empty, ErrorType.None);
}
