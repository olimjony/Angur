using Angur.Domain.Accounts;
using Angur.Domain.Common;

namespace Angur.Application.Abstractions;

public interface IAccountNumberGenerator
{
    Task<AccountNumber> NextAsync(Currency currency, CancellationToken cancellationToken);
}