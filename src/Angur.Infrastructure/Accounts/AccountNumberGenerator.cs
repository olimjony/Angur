using Angur.Application.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;
using Angur.Infrastructure.Database;

using Microsoft.EntityFrameworkCore;

namespace Angur.Infrastructure.Accounts;

internal sealed class AccountNumberGenerator(AngurDbContext dbContext) : IAccountNumberGenerator
{
    public const string SequenceName = "account_number_seq";
    private const string BalanceAccount = "20206";

    public async Task<AccountNumber> NextAsync(Currency currency, CancellationToken cancellationToken)
    {
        long next = await dbContext.Database
            .SqlQuery<long>($"SELECT nextval('account_number_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return AccountNumber.Create($"{BalanceAccount}{NumericCode(currency)}{next:D12}").Value;
    }

    private static string NumericCode(Currency currency) => currency.Code switch
    {
        "TJS" => "972",
        "USD" => "840",
        "EUR" => "978",
        "RUB" => "643",
        _ => throw new InvalidOperationException($"No numeric code configured for currency {currency.Code}."),
    };
}
