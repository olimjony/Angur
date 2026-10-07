using Angur.Application;
using Angur.Infrastructure.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Angur.Infrastructure.IntegrationTests.PostgresFixture))]

namespace Angur.Infrastructure.IntegrationTests;

/// <summary>
/// One real PostgreSQL container for the whole test run (an xUnit assembly fixture).
/// Starts before the first test, applies the real migrations, and is removed after the last test.
/// Every test creates its own data (unique e-mails and account numbers), so tests never clash.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

    private ServiceProvider? _services;

    private ServiceProvider Services =>
        _services ?? throw new InvalidOperationException("The fixture has not been initialized yet.");

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // The same registrations the API uses, pointed at the container.
        _services = BuildServices(_container.GetConnectionString());

        // Migrate (not EnsureCreated): this also proves the migrations themselves are correct.
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AngurDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    /// <summary>
    /// Runs <paramref name="action"/> in a fresh DI scope = a fresh DbContext,
    /// exactly like one HTTP request. Nothing is shared with other scopes except the database.
    /// </summary>
    public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    public Task InScopeAsync(Func<IServiceProvider, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return InScopeAsync(async provider =>
        {
            await action(provider);
            return true;
        });
    }

    /// <summary>Opens a scope the caller controls (for tests that need two scopes at once).</summary>
    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();

    internal static ServiceProvider BuildServices(string connectionString)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = connectionString,
            })
            .Build();

        return new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }
}
