using Angur.Application.Abstractions.Messaging;

namespace Angur.Application.Accounts.Deposit;

public sealed record DepositCommand(Guid AccountId, decimal Amount, string Currency) : ICommand;
