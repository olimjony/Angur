using Angur.Application.Abstractions.Messaging;

namespace Angur.Application.Customers.RegisterCustomer;

public sealed record RegisterCustomerCommand(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth) : ICommand<Guid>;
