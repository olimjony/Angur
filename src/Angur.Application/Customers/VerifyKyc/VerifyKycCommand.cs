using Angur.Application.Abstractions.Messaging;

namespace Angur.Application.Customers.VerifyKyc;

public sealed record VerifyKycCommand(Guid CustomerId) : ICommand;
