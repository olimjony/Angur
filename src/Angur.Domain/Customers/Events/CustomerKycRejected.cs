using Angur.Domain.Abstractions;

namespace Angur.Domain.Customers.Events;

public sealed record CustomerKycRejected(CustomerId CustomerId, string Reason) : IDomainEvent;