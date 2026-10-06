using Angur.Application.Abstractions.Messaging;

namespace Angur.Application.Accounts.CloseAccount;

public sealed record CloseAccountCommand(Guid AccountId) : ICommand;
