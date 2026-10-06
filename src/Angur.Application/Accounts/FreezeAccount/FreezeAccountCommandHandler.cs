using Angur.Application.Abstractions;
using Angur.Application.Abstractions.Messaging;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;

namespace Angur.Application.Accounts.FreezeAccount;

internal sealed class FreezeAccountCommandHandler(
    IAccountRepository accountRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<FreezeAccountCommand>
{
    public async Task<Result> HandleAsync(FreezeAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await accountRepository.GetByIdAsync(new AccountId(command.AccountId), cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound);
        }

        Result result = account.Freeze(command.Reason, timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
