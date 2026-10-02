using Angur.Domain.Abstractions;

namespace Angur.Domain.UnitTests.Abstractions;

public class AggregateRootTests
{
    [Fact]
    public void GetDomainEvents_ReturnsSnapshot_NotAffectedByLaterEvents()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());
        aggregate.DoSomething();

        // Act
        IReadOnlyList<IDomainEvent> snapshot = aggregate.GetDomainEvents();
        aggregate.DoSomething();

        // Assert
        Assert.Single(snapshot);
        Assert.Equal(2, aggregate.GetDomainEvents().Count);
    }

    [Fact]
    public void Raise_AddsEventToDomainEvents()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());

        // Act
        aggregate.DoSomething();
        IReadOnlyList<IDomainEvent> snapshot = aggregate.GetDomainEvents();

        // Assert
        Assert.Single(snapshot);
        Assert.IsType<SomethingHappened>(snapshot[0]);        
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());
        aggregate.DoSomething();
        aggregate.DoSomething();

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        Assert.Empty(aggregate.GetDomainEvents());        
    }

    [Fact]
    public void NewAggregate_HasNoDomainEvents()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());

        // Act
        IReadOnlyList<IDomainEvent> snapshot = aggregate.GetDomainEvents();

        // Assert
        Assert.Empty(snapshot);
    }

    [Fact]
    public void Raise_MultipleEvents_PreservesOrder()
    {
        // Arrange
        TestAggregate aggregate = new(Guid.NewGuid());
        aggregate.DoSomething();
        aggregate.DoSomethingElse();

        // Act
        IReadOnlyList<IDomainEvent> snapshot = aggregate.GetDomainEvents();

        // Assert
        Assert.Collection(snapshot, first => Assert.IsType<SomethingHappened>(first),
                                    second => Assert.IsType<SomethingElseHappened>(second));

    }
    private sealed record SomethingHappened(Guid AggregateId) : IDomainEvent;

    private sealed record SomethingElseHappened(Guid AggregateId) : IDomainEvent;

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void DoSomething() => Raise(new SomethingHappened(Id));

        public void DoSomethingElse() => Raise(new SomethingElseHappened(Id));
    }
}
