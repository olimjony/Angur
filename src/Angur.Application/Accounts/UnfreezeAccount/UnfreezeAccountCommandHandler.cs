using Angur.Application.Abstractions;
using Angur.Application.Abstractions.Messaging;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;

namespace Angur.Application.Accounts.UnfreezeAccount;

internal sealed class UnfreezeAccountCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UnfreezeAccountCommand>
{
    public async Task<Result> HandleAsync(UnfreezeAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await accountRepository.GetByIdAsync(new AccountId(command.AccountId), cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound);
        }

        Result result = account.Unfreeze();
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
