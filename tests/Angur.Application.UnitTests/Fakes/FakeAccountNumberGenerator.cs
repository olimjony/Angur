using Angur.Application.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;

namespace Angur.Application.UnitTests.Fakes;

/// <summary>Always hands out the same number and remembers which currencies were requested.</summary>
internal sealed class FakeAccountNumberGenerator : IAccountNumberGenerator
{
    public static readonly AccountNumber Number = AccountNumber.Create("20206840000000000001").Value;

    private readonly List<Currency> _requests = [];

    public IReadOnlyList<Currency> Requests => _requests;

    public Task<AccountNumber> NextAsync(Currency currency, CancellationToken cancellationToken)
    {
        _requests.Add(currency);
        return Task.FromResult(Number);
    }
}
