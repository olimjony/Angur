using Angur.Domain.Abstractions;

namespace Angur.Domain.Accounts.Events;

public sealed record AccountClosed(AccountId AccountId) : IDomainEvent;
