using Angur.Domain.Accounts;
using Angur.Domain.Common;
using Angur.Domain.Customers;

namespace Angur.Application.UnitTests;

/// <summary>Ready-made aggregates in every state the handlers care about.</summary>
internal static class TestData
{
    /// <summary>The moment every handler "runs" at (what FakeTimeProvider returns).</summary>
    public static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    /// <summary>When the seeded customers and accounts were created.</summary>
    public static readonly DateTimeOffset Earlier = Now.AddDays(-30);

    public static readonly AccountNumber Number = AccountNumber.Create("20206972000000012345").Value;

    public static Customer PendingCustomer(string email = "ali@mail.tj")
    {
        Customer customer = Customer.Register(
            FullName.Create("Ali", "Karimov").Value,
            Email.Create(email).Value,
            new DateOnly(1990, 6, 1),
            Earlier).Value;

        customer.ClearDomainEvents();
        return customer;
    }

    public static Customer VerifiedCustomer()
    {
        Customer customer = PendingCustomer();
        customer.VerifyKyc(Earlier);
        customer.ClearDomainEvents();
        return customer;
    }

    public static Customer RejectedCustomer()
    {
        Customer customer = PendingCustomer();
        customer.RejectKyc("Documents are not valid", Earlier);
        customer.ClearDomainEvents();
        return customer;
    }

    /// <summary>An active TJS account with <paramref name="balance"/> on it.</summary>
    public static Account ActiveAccount(decimal balance = 0m)
    {
        Account account = Account.Open(VerifiedCustomer(), Number, Currency.TJS, Earlier).Value;

        if (balance > 0m)
        {
            account.Deposit(Tjs(balance));
        }

        account.ClearDomainEvents();
        return account;
    }

    public static Account FrozenAccount(decimal balance = 0m)
    {
        Account account = ActiveAccount(balance);
        account.Freeze("Suspicious activity", Earlier);
        account.ClearDomainEvents();
        return account;
    }

    public static Account ClosedAccount()
    {
        Account account = ActiveAccount();
        account.Close(Earlier);
        account.ClearDomainEvents();
        return account;
    }

    public static Money Tjs(decimal amount) => Money.Create(amount, Currency.TJS).Value;
}
