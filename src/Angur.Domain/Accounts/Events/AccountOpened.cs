using Angur.Domain.Abstractions;
using Angur.Domain.Customers;

namespace Angur.Domain.Accounts.Events;

public sealed record AccountOpened(AccountId AccountId, CustomerId CustomerId) : IDomainEvent;
