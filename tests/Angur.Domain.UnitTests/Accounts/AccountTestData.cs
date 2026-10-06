using Angur.Domain.Accounts;
using Angur.Domain.Common;
using Angur.Domain.Customers;

namespace Angur.Domain.UnitTests.Accounts;

internal static class AccountTestData
{
    public static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    public static readonly AccountNumber Number = AccountNumber.Create("20206972000000012345").Value;

    public static Customer VerifiedCustomer()
    {
        Customer customer = Customer.Register(
            FullName.Create("Ali", "Karimov").Value,
            Email.Create("ali@mail.tj").Value,
            new DateOnly(1990, 6, 1),
            Now.AddDays(-10)).Value;

        customer.VerifyKyc(Now.AddDays(-5));
        return customer;
    }

    public static Account OpenAccount(Currency currency) =>
        Account.Open(VerifiedCustomer(), Number, currency, Now).Value;

    /// <summary>Opens a TJS account, puts <paramref name="balance"/> on it and clears the events.</summary>
    public static Account TjsAccountWithBalance(decimal balance)
    {
        Account account = OpenAccount(Currency.TJS);

        if (balance > 0m)
        {
            account.Deposit(Tjs(balance));
        }

        account.ClearDomainEvents();
        return account;
    }

    public static Money Tjs(decimal amount) => Money.Create(amount, Currency.TJS).Value;

    public static Money Usd(decimal amount) => Money.Create(amount, Currency.USD).Value;
}
