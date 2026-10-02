using Angur.Domain.Abstractions;

using Xunit.Sdk;

namespace Angur.Domain.UnitTests.Abstractions;

public class EntityTests
{
    [Fact]
    public void Equals_SameIdAndSameType_ReturnsTrue()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestEntity first = new(id);
        TestEntity second = new(id);

        // Act
        bool areEqual = first.Equals(second);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_SameIdAndDifferentType_ReturnsFalse()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestEntity entity = new(id);
        OtherEntity other = new (id);

        // Act
        bool areEqual = entity.Equals(other);


        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        TestEntity entity1 = new(id1);
        TestEntity entity2 = new(id2);

        // Act
        bool areEqual = entity1.Equals(entity2);

        // Assert
        Assert.False(areEqual);

    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestEntity entity = new TestEntity(id);

        // Act
        bool areEqual = entity.Equals(null);

        // Assert
        Assert.False(areEqual);

    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        TestEntity? entity1 = null, entity2 = null;

        // Act
        bool areEqual = entity1 == entity2;

        // Assert
        Assert.True(areEqual);
        
    }

    [Fact]
    public void EqualityOperator_SameId_ReturnsTrue()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestEntity entity1 = new TestEntity(id);
        TestEntity entity2 = new TestEntity(id);
        
        // Act
        bool areEqual = entity1 == entity2;

        // Assert
        Assert.True(areEqual);
    }

    
    [Fact]
    public void GetHashCode_EqualEntities_ReturnSameHash()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestEntity entity1 = new TestEntity(id);
        TestEntity entity2 = new TestEntity(id);
        
        // Act
        bool areEqual = entity1.GetHashCode() == entity2.GetHashCode();

        // Assert
        Assert.True(areEqual);
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class OtherEntity(Guid id): Entity<Guid>(id);
}
