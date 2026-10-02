using Angur.Domain.Abstractions;

namespace Angur.Domain.UnitTests.Abstractions;

public class ResultTests
{
    private static readonly DomainError TestError =
        new("Test.Error", "Something went wrong.", ErrorType.Failure);

    // ---------- Result (non-generic) ----------

    [Fact]
    public void Success_NonGeneric_IsSuccessWithNoneError()
    {
        // Act
        Result result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(DomainError.None, result.Error);
    }

    [Fact]
    public void Failure_WithError_IsFailureAndCarriesError()
    {
        // Act
        Result result = Result.Failure(TestError);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void Failure_WithNoneError_ThrowsArgumentException()
    {
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => Result.Failure(DomainError.None));

        Assert.Equal("error", exception.ParamName);
    }

    [Fact]
    public void Constructor_SuccessWithError_ThrowsArgumentException()
    {
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => new SuccessWithErrorResult());

        Assert.Equal("error", exception.ParamName);
    }

    // ---------- Result<TValue> ----------

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public void Success_WithValue_ExposesValueAndNoError(int value)
    {
        // Act
        Result<int> result = Result.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(value, result.Value);
        Assert.Equal(DomainError.None, result.Error);
    }

    [Fact]
    public void FailureOfT_WithError_IsFailureAndCarriesError()
    {
        // Act
        Result<int> result = Result.Failure<int>(TestError);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void FailureOfT_WithNoneError_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure<int>(DomainError.None));
    }

    [Fact]
    public void Value_OnFailedResult_ThrowsInvalidOperationException()
    {
        // Arrange
        Result<int> result = Result.Failure<int>(TestError);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    // ---------- Test doubles ----------

    private sealed class SuccessWithErrorResult() : Result(true, TestError);
}
