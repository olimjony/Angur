namespace Angur.Domain.Abstractions;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }

    public IReadOnlyList<IDomainEvent> GetDomainEvents() => [.. _domainEvents];
    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
