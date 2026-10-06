namespace Angur.Domain.Accounts;

public readonly record struct AccountId(Guid Value)
{
    public static AccountId New() => new(Guid.CreateVersion7());
}
