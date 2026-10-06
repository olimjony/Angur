using Angur.Application.Abstractions;
using Angur.Application.Abstractions.Messaging;
using Angur.Domain.Abstractions;
using Angur.Domain.Customers;

namespace Angur.Application.Customers.RegisterCustomer;

internal sealed class RegisterCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterCustomerCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        Result<FullName> name = FullName.Create(command.FirstName, command.LastName);
        if (name.IsFailure)
        {
            return Result.Failure<Guid>(name.Error);
        }

        Result<Email> email = Email.Create(command.Email);
        if (email.IsFailure)
        {
            return Result.Failure<Guid>(email.Error);
        }

        if (await customerRepository.EmailExistsAsync(email.Value, cancellationToken))
        {
            return Result.Failure<Guid>(CustomerErrors.EmailAlreadyRegistered);
        }

        Result<Customer> customer = Customer.Register(name.Value, email.Value, command.DateOfBirth, timeProvider.GetUtcNow());
        if (customer.IsFailure)
        {
            return Result.Failure<Guid>(customer.Error);
        }

        customerRepository.Add(customer.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(customer.Value.Id.Value);
    }
}
