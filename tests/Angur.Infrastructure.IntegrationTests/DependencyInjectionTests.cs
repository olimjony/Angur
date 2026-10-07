using Angur.Application.Abstractions;
using Angur.Infrastructure.Accounts;
using Angur.Infrastructure.Database;
using Angur.Infrastructure.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Angur.Infrastructure.IntegrationTests;

/// <summary>
/// These tests do not need a running database: building the container and creating
/// objects does not open a connection. They catch wiring mistakes before `dotnet ef` or the API does.
/// </summary>
public class DependencyInjectionTests
{
    private const string AnyConnectionString = "Host=localhost;Database=not_used";

    [Fact]
    public void AddApplicationAndInfrastructure_EveryServiceCanBeBuilt()
    {
        // ValidateOnBuild inside BuildServices throws if any registration cannot be constructed,
        // e.g. AddScoped<IAccountRepository, IAccountRepository>() or a missing port.
        using ServiceProvider provider = PostgresFixture.BuildServices(AnyConnectionString);

        Assert.NotNull(provider);
    }

    [Fact]
    public void EveryApplicationPort_IsImplementedByInfrastructure()
    {
        using ServiceProvider provider = PostgresFixture.BuildServices(AnyConnectionString);
        using IServiceScope scope = provider.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        Assert.IsType<CustomerRepository>(services.GetRequiredService<ICustomerRepository>());
        Assert.IsType<AccountRepository>(services.GetRequiredService<IAccountRepository>());
        Assert.IsType<AccountNumberGenerator>(services.GetRequiredService<IAccountNumberGenerator>());
        Assert.IsType<AngurDbContext>(services.GetRequiredService<IUnitOfWork>());
        Assert.Same(TimeProvider.System, services.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void UnitOfWork_IsTheSameDbContextTheRepositoriesUse()
    {
        // If IUnitOfWork were registered as a SECOND DbContext, repositories would track changes
        // in one context and SaveChanges would run on another, empty one: nothing would be saved.
        using ServiceProvider provider = PostgresFixture.BuildServices(AnyConnectionString);
        using IServiceScope scope = provider.CreateScope();

        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        AngurDbContext dbContext = scope.ServiceProvider.GetRequiredService<AngurDbContext>();

        Assert.Same(dbContext, unitOfWork);
    }

    [Fact]
    public void DbContext_IsDifferentInEveryScope()
    {
        // Scoped = one DbContext per request. Sharing one between requests would mix their changes.
        using ServiceProvider provider = PostgresFixture.BuildServices(AnyConnectionString);
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();

        Assert.NotSame(
            first.ServiceProvider.GetRequiredService<AngurDbContext>(),
            second.ServiceProvider.GetRequiredService<AngurDbContext>());
    }

    [Fact]
    public void AddInfrastructure_WithoutConnectionString_FailsFastWithClearMessage()
    {
        IConfiguration empty = new ConfigurationBuilder().Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(empty));

        Assert.Contains("Database", exception.Message, StringComparison.Ordinal);
    }
}
