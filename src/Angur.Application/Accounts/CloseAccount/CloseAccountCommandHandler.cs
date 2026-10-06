using Angur.Application.Abstractions;
using Angur.Application.Abstractions.Messaging;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;

namespace Angur.Application.Accounts.CloseAccount;

internal sealed class CloseAccountCommandHandler(
    IAccountRepository accountRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseAccountCommand>
{
    public async Task<Result> HandleAsync(CloseAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await accountRepository.GetByIdAsync(new AccountId(command.AccountId), cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound);
        }

        Result result = account.Close(timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
