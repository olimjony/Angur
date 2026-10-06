using Angur.Domain.Abstractions;
using Angur.Domain.Common;

namespace Angur.Domain.Accounts.Events;

public sealed record MoneyDeposited(AccountId AccountId, Money Amount, Money BalanceAfter) : IDomainEvent;

