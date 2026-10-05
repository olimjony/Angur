using Angur.Domain.Abstractions;

namespace Angur.Domain.UnitTests.Abstractions;

public class DomainErrorTests
{
    [Fact]
    public void Errors_WithSameValues_AreEqual()
    {
        DomainError first = new("Account.Frozen", "The account is frozen.", ErrorType.Conflict);
        DomainError second = new("Account.Frozen", "The account is frozen.", ErrorType.Conflict);

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }
   
    [Fact]
    public void Errors_WithDifferentCode_AreNotEqual()
    {
        // Arrange
        DomainError first = new("Account.Frozen", "The account is frozen", ErrorType.Conflict);
        DomainError second = new("Account.Freeze", "The account is frozen", ErrorType.Conflict);

        // Assert
        Assert.Equal(first.Description, second.Description);
        Assert.Equal(first.ErrorType, second.ErrorType);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void None_HasEmptyCodeAndNoneType()
    {
        // Arrange
        DomainError error = DomainError.None;


        // Assert
        Assert.Equal(string.Empty, error.Code);
        Assert.Equal(string.Empty, error.Description);
        Assert.Equal(ErrorType.None, error.ErrorType);
        
    }
}
