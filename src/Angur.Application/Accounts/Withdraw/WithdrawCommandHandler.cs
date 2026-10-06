using Angur.Application.Abstractions;
using Angur.Application.Abstractions.Messaging;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;

namespace Angur.Application.Accounts.Withdraw;

internal sealed class WithdrawCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<WithdrawCommand>
{
    public async Task<Result> HandleAsync(WithdrawCommand command, CancellationToken cancellationToken)
    {
        Result<Currency> currency = Currency.FromCode(command.Currency);
        if (currency.IsFailure)
        {
            return currency;
        }

        Result<Money> amount = Money.Create(command.Amount, currency.Value);
        if (amount.IsFailure)
        {
            return amount;
        }

        Account? account = await accountRepository.GetByIdAsync(new AccountId(command.AccountId), cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound);
        }

        Result result = account.Withdraw(amount.Value);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
