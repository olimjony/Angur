using Angur.Infrastructure.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Angur.Infrastructure.IntegrationTests;

public class MigrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Model_HasNoChangesMissingFromMigrations()
    {
        // Fails when someone changes a configuration (a column, an index, a new property)
        // but forgets to run `dotnet ef migrations add`.
        bool hasPendingChanges = await fixture.InScopeAsync(provider => Task.FromResult(
            provider.GetRequiredService<AngurDbContext>().Database.HasPendingModelChanges()));

        Assert.False(hasPendingChanges, "The model has changes that are not in a migration. Run `dotnet ef migrations add <Name>`.");
    }

    [Fact]
    public async Task Database_HasEveryMigrationApplied()
    {
        IEnumerable<string> pending = await fixture.InScopeAsync(provider =>
            provider.GetRequiredService<AngurDbContext>().Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));

        Assert.Empty(pending);
    }
}
