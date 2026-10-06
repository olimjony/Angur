using Angur.Domain.Abstractions;
using Angur.Domain.Customers;

namespace Angur.Domain.UnitTests.Customers;

public class FullNameTests
{
    // ---------- Required parts ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingFirstName_ReturnsFirstNameRequired(string? firstName)
    {
        // Act
        Result<FullName> result = FullName.Create(firstName, "Karimov");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FullNameErrors.FirstNameRequired, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingLastName_ReturnsLastNameRequired(string? lastName)
    {
        // Act
        Result<FullName> result = FullName.Create("Ali", lastName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FullNameErrors.LastNameRequired, result.Error);
    }

    [Fact]
    public void Create_BothPartsMissing_ReportsFirstNameFirst()
    {
        Result<FullName> result = FullName.Create(null, null);

        Assert.Equal(FullNameErrors.FirstNameRequired, result.Error);
    }

    // ---------- Valid names ----------

    [Fact]
    public void Create_ValidParts_ReturnsTrimmedFullName()
    {
        // Act
        Result<FullName> result = FullName.Create("  Ali ", " Karimov  ");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Ali", result.Value.FirstName);
        Assert.Equal("Karimov", result.Value.LastName);
    }

    [Fact]
    public void ToString_ReturnsFirstAndLastName()
    {
        FullName name = FullName.Create("Ali", "Karimov").Value;

        Assert.Equal("Ali Karimov", name.ToString());
    }

    // ---------- Length ----------

    [Fact]
    public void Create_PartsAtMaxLength_Succeeds()
    {
        string longest = new('a', FullName.MaxPartLength);

        Result<FullName> result = FullName.Create(longest, longest);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_FirstNameOverMaxLength_ReturnsTooLong()
    {
        string tooLong = new('a', FullName.MaxPartLength + 1);

        Result<FullName> result = FullName.Create(tooLong, "Karimov");

        Assert.Equal(FullNameErrors.TooLong, result.Error);
    }

    [Fact]
    public void Create_LastNameOverMaxLength_ReturnsTooLong()
    {
        string tooLong = new('a', FullName.MaxPartLength + 1);

        Result<FullName> result = FullName.Create("Ali", tooLong);

        Assert.Equal(FullNameErrors.TooLong, result.Error);
    }

    [Fact]
    public void Create_MaxLengthPlusSurroundingSpaces_Succeeds()
    {
        // Length is checked after trimming, so the spaces don't count.
        string longestWithSpaces = $"  {new string('a', FullName.MaxPartLength)}  ";

        Result<FullName> result = FullName.Create(longestWithSpaces, "Karimov");

        Assert.True(result.IsSuccess);
    }

    // ---------- Equality ----------

    [Fact]
    public void Equals_SameParts_AreEqual()
    {
        FullName first = FullName.Create("Ali", "Karimov").Value;
        FullName second = FullName.Create(" Ali ", "Karimov").Value;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentLastName_AreNotEqual()
    {
        FullName first = FullName.Create("Ali", "Karimov").Value;
        FullName second = FullName.Create("Ali", "Rahimov").Value;

        Assert.NotEqual(first, second);
    }
}
