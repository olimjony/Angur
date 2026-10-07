using Angur.Application.Abstractions.Messaging;
using Angur.Application.Accounts.Deposit;
using Angur.Application.Accounts.OpenAccount;
using Angur.Application.Accounts.Withdraw;
using Angur.Application.Customers.RegisterCustomer;
using Angur.Application.Customers.VerifyKyc;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;
using Angur.Domain.Customers;

using Microsoft.Extensions.DependencyInjection;

using static Angur.Infrastructure.IntegrationTests.TestData;

namespace Angur.Infrastructure.IntegrationTests;

/// <summary>
/// The real handlers from Application on top of the real Infrastructure and PostgreSQL:
/// the same path a request will take once the API exists. Each command runs in its own scope.
/// </summary>
public class CommandPipelineTests(PostgresFixture fixture)
{
    [Fact]
    public async Task RegisterVerifyOpenDepositWithdraw_EndToEnd_BalanceIsPersisted()
    {
        // Act
        Result<Guid> customerId = await SendAsync<RegisterCustomerCommand, Guid>(
            new RegisterCustomerCommand("Ali", "Karimov", UniqueEmail(), new DateOnly(1990, 6, 1)));
        Result verified = await SendAsync(new VerifyKycCommand(customerId.Value));
        Result<Guid> accountId = await SendAsync<OpenAccountCommand, Guid>(new OpenAccountCommand(customerId.Value, "TJS"));
        Result deposited = await SendAsync(new DepositCommand(accountId.Value, 1000m, "TJS"));
        Result withdrawn = await SendAsync(new WithdrawCommand(accountId.Value, 250.50m, "TJS"));

        // Assert: every step succeeded...
        Assert.True(customerId.IsSuccess);
        Assert.True(verified.IsSuccess);
        Assert.True(accountId.IsSuccess);
        Assert.True(deposited.IsSuccess);
        Assert.True(withdrawn.IsSuccess);

        // ...and the database has the result.
        Account? account = await fixture.LoadAsync(new AccountId(accountId.Value));
        Assert.Equal(Tjs(749.50m), account!.Balance);
        Assert.Equal(new CustomerId(customerId.Value), account.CustomerId);
        Assert.StartsWith("20206972", account.Number.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterTwiceWithSameEmail_SecondReturnsEmailAlreadyRegistered()
    {
        string email = UniqueEmail();
        await SendAsync<RegisterCustomerCommand, Guid>(new RegisterCustomerCommand("Ali", "Karimov", email, new DateOnly(1990, 6, 1)));

        Result<Guid> second = await SendAsync<RegisterCustomerCommand, Guid>(
            new RegisterCustomerCommand("Vali", "Rahimov", email.ToUpperInvariant(), new DateOnly(1985, 1, 1)));

        Assert.Equal(CustomerErrors.EmailAlreadyRegistered, second.Error);
    }

    [Fact]
    public async Task FailedWithdrawal_DoesNotChangeTheStoredBalance()
    {
        Account account = await fixture.SaveAccountAsync(arrange: a => a.Deposit(Tjs(100m)));

        Result result = await SendAsync(new WithdrawCommand(account.Id.Value, 100.01m, "TJS"));

        Assert.Equal(AccountErrors.InsufficientFunds, result.Error);
        Account? reloaded = await fixture.LoadAsync(account.Id);
        Assert.Equal(Tjs(100m), reloaded!.Balance);
    }

    [Fact]
    public async Task OpenAccount_ForUnverifiedCustomer_SavesNothing()
    {
        Result<Guid> customerId = await SendAsync<RegisterCustomerCommand, Guid>(
            new RegisterCustomerCommand("Ali", "Karimov", UniqueEmail(), new DateOnly(1990, 6, 1)));

        Result<Guid> result = await SendAsync<OpenAccountCommand, Guid>(new OpenAccountCommand(customerId.Value, "USD"));

        Assert.Equal(AccountErrors.CustomerNotVerified, result.Error);
    }

    // ---------- Helpers: resolve the handler from DI, the way the API will ----------

    private Task<Result> SendAsync<TCommand>(TCommand command)
        where TCommand : ICommand =>
        fixture.InScopeAsync(provider => provider.GetRequiredService<ICommandHandler<TCommand>>()
            .HandleAsync(command, TestContext.Current.CancellationToken));

    private Task<Result<TResponse>> SendAsync<TCommand, TResponse>(TCommand command)
        where TCommand : ICommand<TResponse> =>
        fixture.InScopeAsync(provider => provider.GetRequiredService<ICommandHandler<TCommand, TResponse>>()
            .HandleAsync(command, TestContext.Current.CancellationToken));
}
