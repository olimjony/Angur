using Angur.Application.Abstractions.Messaging;

namespace Angur.Application.Accounts.OpenAccount;

public sealed record OpenAccountCommand(Guid CustomerId, string Currency) : ICommand<Guid>;
