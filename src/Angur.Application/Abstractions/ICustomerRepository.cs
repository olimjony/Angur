using Angur.Domain.Customers;

namespace Angur.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken);
    void Add(Customer customer);
}