using Angur.Application.Abstractions.Messaging;

namespace Angur.Application.Accounts.Withdraw;

public sealed record WithdrawCommand(Guid AccountId, decimal Amount, string Currency) : ICommand;
