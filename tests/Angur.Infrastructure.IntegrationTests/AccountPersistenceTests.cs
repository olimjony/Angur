using Angur.Application.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;
using Angur.Domain.Customers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using static Angur.Infrastructure.IntegrationTests.TestData;

namespace Angur.Infrastructure.IntegrationTests;

public class AccountPersistenceTests(PostgresFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---------- Round trip ----------

    [Fact]
    public async Task SavedAccount_LoadedInNewScope_HasEveryFieldRestored()
    {
        // Arrange
        Customer owner = VerifiedCustomer();
        await fixture.SaveAsync(owner);
        Account saved = NewAccount(owner, Currency.USD);

        // Act
        await fixture.SaveAsync(saved);
        Account? loaded = await fixture.LoadAsync(saved.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.NotSame(saved, loaded);
        Assert.Equal(saved.Id, loaded.Id);
        Assert.Equal(owner.Id, loaded.CustomerId);
        Assert.Equal(saved.Number, loaded.Number);
        Assert.Same(Currency.USD, loaded.Currency); // the converter returns the same smart-enum instance
        Assert.Equal(Money.Zero(Currency.USD), loaded.Balance);
        Assert.Equal(AccountStatus.Active, loaded.Status);
        Assert.Equal(Now, loaded.OpenedAt);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        Account? loaded = await fixture.LoadAsync(new AccountId(Guid.NewGuid()));

        Assert.Null(loaded);
    }

    // ---------- Money keeps every kopeck ----------

    [Fact]
    public async Task DepositAndWithdraw_OnLoadedAccount_ArePersistedExactly()
    {
        // Arrange
        Account account = await fixture.SaveAccountAsync();

        // Act
        await ChangeAsync(account.Id, loaded =>
        {
            loaded.Deposit(Tjs(150.25m));
            loaded.Withdraw(Tjs(50.01m));
        });

        // Assert: decimal -> numeric(19,4) -> decimal without any rounding.
        Account? reloaded = await fixture.LoadAsync(account.Id);
        Assert.Equal(Tjs(100.24m), reloaded!.Balance);
    }

    // ---------- Lifecycle changes are persisted ----------

    [Fact]
    public async Task Freeze_OnLoadedAccount_IsPersistedWithReasonAndTime()
    {
        Account account = await fixture.SaveAccountAsync();

        await ChangeAsync(account.Id, loaded => loaded.Freeze("Court order 15/2026", Now.AddDays(1)));

        Account? reloaded = await fixture.LoadAsync(account.Id);
        Assert.Equal(AccountStatus.Frozen, reloaded!.Status);
        Assert.Equal("Court order 15/2026", reloaded.FreezeReason);
        Assert.Equal(Now.AddDays(1), reloaded.FrozenAt);
    }

    [Fact]
    public async Task Close_OnLoadedAccount_IsPersistedWithTime()
    {
        Account account = await fixture.SaveAccountAsync();

        await ChangeAsync(account.Id, loaded => loaded.Close(Now.AddDays(1)));

        Account? reloaded = await fixture.LoadAsync(account.Id);
        Assert.Equal(AccountStatus.Closed, reloaded!.Status);
        Assert.Equal(Now.AddDays(1), reloaded.ClosedAt);
    }

    // ---------- Database constraints ----------

    [Fact]
    public async Task AccountOfUnknownCustomer_IsRejectedByForeignKey()
    {
        // The customer exists only in memory, never saved.
        Account orphan = NewAccount(VerifiedCustomer(), Currency.TJS);

        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(() => fixture.SaveAsync(orphan));

        PostgresException postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgres.SqlState);
    }

    [Fact]
    public async Task TwoAccountsWithSameNumber_AreRejectedByUniqueIndex()
    {
        Account first = await fixture.SaveAccountAsync();
        Customer owner = VerifiedCustomer();
        await fixture.SaveAsync(owner);
        Account duplicate = Account.Open(owner, first.Number, Currency.TJS, Now).Value;

        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(() => fixture.SaveAsync(duplicate));

        PostgresException postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("ix_accounts_number", postgres.ConstraintName);
    }

    // ---------- Optimistic concurrency: no double spending ----------

    [Fact]
    public async Task TwoConcurrentWithdrawals_SecondSaveFailsAndMoneyIsNotSpentTwice()
    {
        // Arrange: 100 TJS on the account.
        Account account = await fixture.SaveAccountAsync(arrange: a => a.Deposit(Tjs(100m)));

        // Two requests load the same account at the same time.
        await using var first = fixture.CreateScope();
        await using var second = fixture.CreateScope();
        Account inFirst = (await first.ServiceProvider.GetRequiredService<IAccountRepository>().GetByIdAsync(account.Id, Ct))!;
        Account inSecond = (await second.ServiceProvider.GetRequiredService<IAccountRepository>().GetByIdAsync(account.Id, Ct))!;

        // Each one sees 100 in memory, so each 60 withdrawal passes the domain check.
        Assert.True(inFirst.Withdraw(Tjs(60m)).IsSuccess);
        Assert.True(inSecond.Withdraw(Tjs(60m)).IsSuccess);

        // Act: the first commit wins...
        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);

        // ...the second sees that the row changed since it was read (xmin) and is refused.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct));

        // Assert: 40 left, not -20.
        Account? reloaded = await fixture.LoadAsync(account.Id);
        Assert.Equal(Tjs(40m), reloaded!.Balance);
    }

    // ---------- Helpers ----------

    /// <summary>Load -> change -> save in one scope, exactly like a command handler.</summary>
    private Task ChangeAsync(AccountId id, Action<Account> change) =>
        fixture.InScopeAsync(async provider =>
        {
            Account account = (await provider.GetRequiredService<IAccountRepository>().GetByIdAsync(id, Ct))!;
            change(account);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        });
}
