using Angur.Application.Abstractions;
using Angur.Application.Abstractions.Messaging;
using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Common;
using Angur.Domain.Customers;

namespace Angur.Application.Accounts.OpenAccount;

internal sealed class OpenAccountCommandHandler(ICustomerRepository customerRepository,
    IAccountRepository accountRepository,
    IAccountNumberGenerator accountNumberGenerator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<OpenAccountCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(OpenAccountCommand command, CancellationToken cancellationToken)
    {
        Result<Currency> currency = Currency.FromCode(command.Currency);
        if (currency.IsFailure)
        {
            return Result.Failure<Guid>(currency.Error);
        }

        Customer? customer = await customerRepository.GetByIdAsync(new CustomerId(command.CustomerId), cancellationToken);
        if (customer is null)
        {
            return Result.Failure<Guid>(CustomerErrors.NotFound);
        }

        AccountNumber accountNumber = await accountNumberGenerator.NextAsync(currency.Value, cancellationToken);

        Result<Account> account = Account.Open(customer, accountNumber, currency.Value, timeProvider.GetUtcNow());
        if (account.IsFailure)
        {
            return Result.Failure<Guid>(account.Error);
        }

        accountRepository.Add(account.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(account.Value.Id.Value);
    }
}
