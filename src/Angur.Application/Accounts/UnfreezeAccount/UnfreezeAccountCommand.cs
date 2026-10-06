using Angur.Application.Abstractions.Messaging;

namespace Angur.Application.Accounts.UnfreezeAccount;

public sealed record UnfreezeAccountCommand(Guid AccountId) : ICommand;
