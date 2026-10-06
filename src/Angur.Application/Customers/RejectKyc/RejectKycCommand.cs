using Angur.Application.Abstractions.Messaging;

namespace Angur.Application.Customers.RejectKyc;

public sealed record RejectKycCommand(Guid CustomerId, string Reason) : ICommand;
