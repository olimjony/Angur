using Angur.Domain.Abstractions;
using Angur.Domain.Customers;

namespace Angur.Domain.Accounts.Events;

public sealed record AccountFrozen(AccountId AccountId, string Reason) : IDomainEvent;
