using Angur.Application.Abstractions;

namespace Angur.Application.UnitTests.Fakes;

/// <summary>Counts commits so tests can check "saved exactly once" or "never saved".</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCount++;
        return Task.CompletedTask;
    }
}
