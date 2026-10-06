using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Accounts.Events;
using Angur.Domain.Common;
using Angur.Domain.Customers;

namespace Angur.Domain.UnitTests.Accounts;

public class AccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly AccountNumber Number = AccountNumber.Create("20206972000000012345").Value;

    // ---------- Open: success ----------

    [Fact]
    public void Open_VerifiedCustomer_ReturnsActiveAccountWithZeroBalance()
    {
        // Arrange
        Customer owner = VerifiedCustomer();

        // Act
        Result<Account> result = Account.Open(owner, Number, Currency.TJS, Now);

        // Assert
        Assert.True(result.IsSuccess);
        Account account = result.Value;
        Assert.Equal(owner.Id, account.CustomerId);
        Assert.Equal(Number, account.Number);
        Assert.Same(Currency.TJS, account.Currency);
        Assert.Equal(Money.Zero(Currency.TJS), account.Balance);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(Now, account.OpenedAt);
    }

    [Theory]
    [InlineData("TJS")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("RUB")]
    public void Open_InAnySupportedCurrency_BalanceIsZeroInThatCurrency(string code)
    {
        // Arrange
        Currency currency = Currency.FromCode(code).Value;

        // Act
        Account account = Account.Open(VerifiedCustomer(), Number, currency, Now).Value;

        // Assert
        Assert.Same(currency, account.Currency);
        Assert.Same(currency, account.Balance.Currency);
        Assert.True(account.Balance.IsZero);
    }

    [Fact]
    public void Open_RaisesAccountOpenedForTheReturnedAccount()
    {
        // Arrange
        Customer owner = VerifiedCustomer();

        // Act
        Account account = Account.Open(owner, Number, Currency.TJS, Now).Value;

        // Assert: the event must belong to the SAME account object that was returned.
        IDomainEvent domainEvent = Assert.Single(account.GetDomainEvents());
        AccountOpened opened = Assert.IsType<AccountOpened>(domainEvent);
        Assert.Equal(account.Id, opened.AccountId);
        Assert.Equal(owner.Id, opened.CustomerId);
    }

    [Fact]
    public void Open_TwoAccounts_GetDifferentIds()
    {
        Customer owner = VerifiedCustomer();

        Account first = Account.Open(owner, Number, Currency.TJS, Now).Value;
        Account second = Account.Open(owner, Number, Currency.USD, Now).Value;

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Open_DoesNotChangeTheCustomer()
    {
        // One operation changes one aggregate: opening an account must not touch the customer.
        Customer owner = VerifiedCustomer();
        owner.ClearDomainEvents();

        Account.Open(owner, Number, Currency.TJS, Now);

        Assert.Equal(KycStatus.Verified, owner.KycStatus);
        Assert.Empty(owner.GetDomainEvents());
    }

    // ---------- Open: KYC rule ----------

    [Fact]
    public void Open_CustomerWithPendingKyc_ReturnsCustomerNotVerified()
    {
        // Act
        Result<Account> result = Account.Open(PendingCustomer(), Number, Currency.TJS, Now);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrors.CustomerNotVerified, result.Error);
    }

    [Fact]
    public void Open_CustomerWithRejectedKyc_ReturnsCustomerNotVerified()
    {
        // Act
        Result<Account> result = Account.Open(RejectedCustomer(), Number, Currency.TJS, Now);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrors.CustomerNotVerified, result.Error);
    }

    // ---------- Open: programmer errors ----------

    [Fact]
    public void Open_NullOwner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Account.Open(null!, Number, Currency.TJS, Now));
    }

    [Fact]
    public void Open_NullNumber_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Account.Open(VerifiedCustomer(), null!, Currency.TJS, Now));
    }

    [Fact]
    public void Open_NullCurrency_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Account.Open(VerifiedCustomer(), Number, null!, Now));
    }

    // ---------- Helpers ----------

    private static Customer PendingCustomer() =>
        Customer.Register(
            FullName.Create("Ali", "Karimov").Value,
            Email.Create("ali@mail.tj").Value,
            new DateOnly(1990, 6, 1),
            Now.AddDays(-10)).Value;

    private static Customer VerifiedCustomer()
    {
        Customer customer = PendingCustomer();
        customer.VerifyKyc(Now.AddDays(-5));
        return customer;
    }

    private static Customer RejectedCustomer()
    {
        Customer customer = PendingCustomer();
        customer.RejectKyc("Documents are not valid", Now.AddDays(-5));
        return customer;
    }
}
