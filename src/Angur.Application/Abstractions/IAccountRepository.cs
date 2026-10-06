using Angur.Domain.Accounts;

namespace Angur.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken);
    void Add(Account account);
}
