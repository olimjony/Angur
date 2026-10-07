using Angur.Application.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Customers;

using Microsoft.Extensions.DependencyInjection;

namespace Angur.Infrastructure.IntegrationTests;

/// <summary>Short-hands for "put this into the database" and "read it back in a new request".</summary>
internal static class FixtureExtensions
{
    public static Task SaveAsync(this PostgresFixture fixture, Customer customer) =>
        fixture.InScopeAsync(async provider =>
        {
            provider.GetRequiredService<ICustomerRepository>().Add(customer);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(TestContext.Current.CancellationToken);
        });

    public static Task SaveAsync(this PostgresFixture fixture, Account account) =>
        fixture.InScopeAsync(async provider =>
        {
            provider.GetRequiredService<IAccountRepository>().Add(account);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(TestContext.Current.CancellationToken);
        });

    public static Task<Customer?> LoadAsync(this PostgresFixture fixture, CustomerId id) =>
        fixture.InScopeAsync(provider =>
            provider.GetRequiredService<ICustomerRepository>().GetByIdAsync(id, TestContext.Current.CancellationToken));

    public static Task<Account?> LoadAsync(this PostgresFixture fixture, AccountId id) =>
        fixture.InScopeAsync(provider =>
            provider.GetRequiredService<IAccountRepository>().GetByIdAsync(id, TestContext.Current.CancellationToken));

    /// <summary>Saves a verified customer with one account in the given state.</summary>
    public static async Task<Account> SaveAccountAsync(this PostgresFixture fixture, Action<Account>? arrange = null)
    {
        Customer owner = TestData.VerifiedCustomer();
        await fixture.SaveAsync(owner);

        Account account = TestData.NewAccount(owner, Domain.Common.Currency.TJS);
        arrange?.Invoke(account);
        await fixture.SaveAsync(account);

        return account;
    }
}
