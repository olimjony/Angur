using Angur.Application.Abstractions;
using Angur.Domain.Accounts;

namespace Angur.Application.UnitTests.Fakes;

/// <summary>An in-memory "table" of accounts. Remembers what the handler added.</summary>
internal sealed class FakeAccountRepository : IAccountRepository
{
    private readonly Dictionary<AccountId, Account> _accounts = [];
    private readonly List<Account> _added = [];

    public IReadOnlyList<Account> Added => _added;

    /// <summary>Puts an account into the "database" before the test runs.</summary>
    public void Seed(Account account) => _accounts[account.Id] = account;

    public Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken) =>
        Task.FromResult(_accounts.GetValueOrDefault(id));

    public void Add(Account account)
    {
        _added.Add(account);
        _accounts[account.Id] = account;
    }
}
