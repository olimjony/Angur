using Angur.Application.Abstractions;
using Angur.Application.Abstractions.Messaging;
using Angur.Domain.Abstractions;
using Angur.Domain.Customers;

namespace Angur.Application.Customers.RejectKyc;

internal sealed class RejectKycCommandHandler(
    ICustomerRepository customerRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RejectKycCommand>
{
    public async Task<Result> HandleAsync(RejectKycCommand command, CancellationToken cancellationToken)
    {
        Customer? customer = await customerRepository.GetByIdAsync(new CustomerId(command.CustomerId), cancellationToken);
        if (customer is null)
        {
            return Result.Failure(CustomerErrors.NotFound);
        }

        Result result = customer.RejectKyc(command.Reason, timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
