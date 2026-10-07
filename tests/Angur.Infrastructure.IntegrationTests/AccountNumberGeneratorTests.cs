using Angur.Application.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;

using Microsoft.Extensions.DependencyInjection;

namespace Angur.Infrastructure.IntegrationTests;

public class AccountNumberGeneratorTests(PostgresFixture fixture)
{
    [Theory]
    [InlineData("TJS", "972")]
    [InlineData("USD", "840")]
    [InlineData("EUR", "978")]
    [InlineData("RUB", "643")]
    public async Task Next_StartsWithBalanceAccountAndIsoNumericCurrencyCode(string currencyCode, string numericCode)
    {
        Currency currency = Currency.FromCode(currencyCode).Value;

        AccountNumber number = await NextAsync(currency);

        Assert.Equal(AccountNumber.Length, number.Value.Length);
        Assert.StartsWith($"20206{numericCode}", number.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Next_EveryCurrencyHasANumericCode()
    {
        // A new currency added to Currency without a code here would throw at account opening.
        foreach (Currency currency in Currency.All)
        {
            AccountNumber number = await NextAsync(currency);
            Assert.NotNull(number);
        }
    }

    [Fact]
    public async Task Next_ManyParallelCalls_NeverReturnTheSameNumber()
    {
        // nextval() is atomic, so even concurrent requests get different numbers.
        // (SELECT MAX(...) + 1 would hand out duplicates here.)
        AccountNumber[] numbers = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(_ => NextAsync(Currency.TJS)));

        Assert.Equal(numbers.Length, numbers.Distinct().Count());
    }

    // Every call in its own scope = its own DbContext and connection, like separate requests.
    private Task<AccountNumber> NextAsync(Currency currency) =>
        fixture.InScopeAsync(provider => provider.GetRequiredService<IAccountNumberGenerator>()
            .NextAsync(currency, TestContext.Current.CancellationToken));
}
