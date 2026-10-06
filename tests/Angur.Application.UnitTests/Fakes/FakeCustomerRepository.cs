using Angur.Application.Abstractions;
using Angur.Domain.Customers;

namespace Angur.Application.UnitTests.Fakes;

/// <summary>An in-memory "table" of customers. Remembers what the handler added.</summary>
internal sealed class FakeCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<CustomerId, Customer> _customers = [];
    private readonly List<Customer> _added = [];

    public IReadOnlyList<Customer> Added => _added;

    /// <summary>Puts a customer into the "database" before the test runs.</summary>
    public void Seed(Customer customer) => _customers[customer.Id] = customer;

    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.GetValueOrDefault(id));

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.Values.Any(customer => customer.Email == email));

    public void Add(Customer customer)
    {
        _added.Add(customer);
        _customers[customer.Id] = customer;
    }
}
