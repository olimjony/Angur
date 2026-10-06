using Angur.Domain.Abstractions;

namespace Angur.Domain.Customers.Events;

public sealed record CustomerKycVerified(CustomerId CustomerId) : IDomainEvent;
