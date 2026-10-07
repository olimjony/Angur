using Angur.Domain.Accounts;
using Angur.Domain.Common;
using Angur.Domain.Customers;

namespace Angur.Infrastructure.IntegrationTests;

/// <summary>
/// Builders for aggregates. Every call gives UNIQUE e-mails and account numbers,
/// because all tests share one database with unique indexes on those columns.
/// </summary>
internal static class TestData
{
    /// <summary>
    /// Whole seconds on purpose: PostgreSQL stores microseconds, .NET ticks are 100 ns,
    /// so a value like DateTimeOffset.UtcNow would come back slightly different.
    /// </summary>
    public static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@mail.tj";

    public static AccountNumber UniqueAccountNumber() =>
        AccountNumber.Create($"99999{Random.Shared.NextInt64(0, 1_000_000_000_000_000):D15}").Value;

    public static Customer NewCustomer(string? email = null) =>
        Customer.Register(
            FullName.Create("Ali", "Karimov").Value,
            Email.Create(email ?? UniqueEmail()).Value,
            new DateOnly(1990, 6, 1),
            Now).Value;

    public static Customer VerifiedCustomer()
    {
        Customer customer = NewCustomer();
        customer.VerifyKyc(Now);
        return customer;
    }

    public static Account NewAccount(Customer owner, Currency currency) =>
        Account.Open(owner, UniqueAccountNumber(), currency, Now).Value;

    public static Money Tjs(decimal amount) => Money.Create(amount, Currency.TJS).Value;
}
