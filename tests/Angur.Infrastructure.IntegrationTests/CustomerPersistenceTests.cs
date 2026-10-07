using Angur.Application.Abstractions;
using Angur.Domain.Customers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using static Angur.Infrastructure.IntegrationTests.TestData;

namespace Angur.Infrastructure.IntegrationTests;

public class CustomerPersistenceTests(PostgresFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---------- Round trip: what goes in comes back ----------

    [Fact]
    public async Task SavedCustomer_LoadedInNewScope_HasEveryFieldRestored()
    {
        // Arrange
        Customer saved = NewCustomer();

        // Act
        await fixture.SaveAsync(saved);
        Customer? loaded = await fixture.LoadAsync(saved.Id);

        // Assert: a different object (new DbContext), same data.
        Assert.NotNull(loaded);
        Assert.NotSame(saved, loaded);
        Assert.Equal(saved.Id, loaded.Id);
        Assert.Equal(saved.Name, loaded.Name);
        Assert.Equal(saved.Email, loaded.Email);
        Assert.Equal(saved.DateOfBirth, loaded.DateOfBirth);
        Assert.Equal(saved.RegisteredAt, loaded.RegisteredAt);
        Assert.Equal(KycStatus.Pending, loaded.KycStatus);
        Assert.Null(loaded.KycDecidedAt);
    }

    [Fact]
    public async Task LoadedCustomer_HasNoDomainEvents()
    {
        // Events belong to the operation that raised them; loading from the database raises nothing.
        Customer saved = NewCustomer();
        await fixture.SaveAsync(saved);

        Customer? loaded = await fixture.LoadAsync(saved.Id);

        Assert.Empty(loaded!.GetDomainEvents());
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        Customer? loaded = await fixture.LoadAsync(new CustomerId(Guid.NewGuid()));

        Assert.Null(loaded);
    }

    // ---------- Changes to a loaded aggregate are saved (no Update method needed) ----------

    [Fact]
    public async Task KycVerification_OnLoadedCustomer_IsPersisted()
    {
        // Arrange
        Customer saved = NewCustomer();
        await fixture.SaveAsync(saved);

        // Act: load -> change -> save, in one scope, like a handler does.
        await fixture.InScopeAsync(async provider =>
        {
            Customer customer = (await provider.GetRequiredService<ICustomerRepository>().GetByIdAsync(saved.Id, Ct))!;
            customer.VerifyKyc(Now.AddDays(1));
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        });

        // Assert
        Customer? reloaded = await fixture.LoadAsync(saved.Id);
        Assert.Equal(KycStatus.Verified, reloaded!.KycStatus);
        Assert.Equal(Now.AddDays(1), reloaded.KycDecidedAt);
    }

    [Fact]
    public async Task KycRejection_OnLoadedCustomer_IsPersistedWithReason()
    {
        Customer saved = NewCustomer();
        await fixture.SaveAsync(saved);

        await fixture.InScopeAsync(async provider =>
        {
            Customer customer = (await provider.GetRequiredService<ICustomerRepository>().GetByIdAsync(saved.Id, Ct))!;
            customer.RejectKyc("Passport expired", Now.AddDays(1));
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        });

        Customer? reloaded = await fixture.LoadAsync(saved.Id);
        Assert.Equal(KycStatus.Rejected, reloaded!.KycStatus);
        Assert.Equal("Passport expired", reloaded.KycRejectionReason);
    }

    // ---------- E-mail uniqueness ----------

    [Fact]
    public async Task EmailExists_ForSavedEmail_ReturnsTrueEvenIfTypedDifferently()
    {
        string email = UniqueEmail();
        await fixture.SaveAsync(NewCustomer(email));

        bool exists = await fixture.InScopeAsync(provider => provider.GetRequiredService<ICustomerRepository>()
            .EmailExistsAsync(Email.Create($"  {email.ToUpperInvariant()} ").Value, Ct));

        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExists_ForUnknownEmail_ReturnsFalse()
    {
        bool exists = await fixture.InScopeAsync(provider => provider.GetRequiredService<ICustomerRepository>()
            .EmailExistsAsync(Email.Create(UniqueEmail()).Value, Ct));

        Assert.False(exists);
    }

    [Fact]
    public async Task SecondCustomerWithSameEmail_IsRejectedByUniqueIndex()
    {
        // The last line of defence: two requests can both pass EmailExistsAsync at the same moment.
        // Here we skip that check on purpose and let the database decide.
        string email = UniqueEmail();
        await fixture.SaveAsync(NewCustomer(email));

        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => fixture.SaveAsync(NewCustomer(email)));

        PostgresException postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("ix_customers_email", postgres.ConstraintName);
    }

    // ---------- Optimistic concurrency ----------

    [Fact]
    public async Task TwoConcurrentKycDecisions_SecondSaveFails()
    {
        // Two officers open the same pending customer; one verifies, the other rejects.
        // Without a concurrency token the last save would silently win.
        Customer saved = NewCustomer();
        await fixture.SaveAsync(saved);

        await using var first = fixture.CreateScope();
        await using var second = fixture.CreateScope();

        Customer inFirst = (await first.ServiceProvider.GetRequiredService<ICustomerRepository>().GetByIdAsync(saved.Id, Ct))!;
        Customer inSecond = (await second.ServiceProvider.GetRequiredService<ICustomerRepository>().GetByIdAsync(saved.Id, Ct))!;

        Assert.True(inFirst.VerifyKyc(Now.AddDays(1)).IsSuccess);
        Assert.True(inSecond.RejectKyc("Fake documents", Now.AddDays(1)).IsSuccess);

        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct));

        Customer? reloaded = await fixture.LoadAsync(saved.Id);
        Assert.Equal(KycStatus.Verified, reloaded!.KycStatus);
    }
}
