using Angur.Application.Abstractions.Messaging;

namespace Angur.Application.Accounts.FreezeAccount;

public sealed record FreezeAccountCommand(Guid AccountId, string Reason) : ICommand;
