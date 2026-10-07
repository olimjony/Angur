using Angur.Application.Abstractions;
using Angur.Domain.Accounts;
using Angur.Infrastructure.Database;

using Microsoft.EntityFrameworkCore;

namespace Angur.Infrastructure.Repositories;

internal sealed class AccountRepository(AngurDbContext dbContext) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken) =>
        dbContext.Accounts.SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

    public void Add(Account account) => dbContext.Accounts.Add(account);
}
