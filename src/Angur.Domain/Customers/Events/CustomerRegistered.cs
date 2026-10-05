using Angur.Domain.Abstractions;

namespace Angur.Domain.Customers.Events;
public sealed record CustomerRegistered(CustomerId CustomerId) : IDomainEvent;