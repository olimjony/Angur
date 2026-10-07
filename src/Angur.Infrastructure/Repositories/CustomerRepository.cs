using Angur.Application.Abstractions;
using Angur.Domain.Customers;
using Angur.Infrastructure.Database;

using Microsoft.EntityFrameworkCore;

namespace Angur.Infrastructure.Repositories;

internal sealed class CustomerRepository(AngurDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
        dbContext.Customers.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Customers.AnyAsync(customer => customer.Email == email, cancellationToken);

    public void Add(Customer customer) => dbContext.Customers.Add(customer);
}
