using Angur.Domain.Abstractions;
using Angur.Domain.Customers;

namespace Angur.Domain.UnitTests.Customers;

public class EmailTests
{
    private const string Domain = "@mail.tj";

    // ---------- Required ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhitespace_ReturnsEmailRequired(string? value)
    {
        // Act
        Result<Email> result = Email.Create(value);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(EmailErrors.EmailRequired, result.Error);
    }

    // ---------- Normalization ----------

    [Theory]
    [InlineData("ali@mail.tj", "ali@mail.tj")]
    [InlineData(" Ali@Mail.TJ ", "ali@mail.tj")]
    [InlineData("ALI.KARIMOV@BANK.CO.TJ", "ali.karimov@bank.co.tj")]
    [InlineData("user+tag@mail.tj", "user+tag@mail.tj")]
    public void Create_ValidEmail_ReturnsTrimmedLowercaseValue(string input, string expected)
    {
        // Act
        Result<Email> result = Email.Create(input);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Fact]
    public void Equals_SameAddressDifferentCase_AreEqual()
    {
        // The same mailbox must not be registered twice with different casing.
        Email first = Email.Create("Ali@Mail.TJ").Value;
        Email second = Email.Create("ali@mail.tj").Value;

        Assert.Equal(first, second);
    }

    // ---------- Format ----------

    [Theory]
    [InlineData("ali")]
    [InlineData("ali@")]
    [InlineData("@mail.tj")]
    [InlineData("ali@mail")]
    [InlineData("ali@mail.")]
    [InlineData("a li@mail.tj")]
    [InlineData("ali@@mail.tj")]
    [InlineData("ali@mail@bank.tj")]
    public void Create_InvalidFormat_ReturnsInvalidFormat(string value)
    {
        // Act
        Result<Email> result = Email.Create(value);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(EmailErrors.InvalidFormat, result.Error);
    }

    // ---------- Length ----------

    [Fact]
    public void Create_AtMaxLength_Succeeds()
    {
        string email = BuildEmailOfLength(Email.MaxPartLength);

        Result<Email> result = Email.Create(email);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_OverMaxLength_ReturnsTooLong()
    {
        string email = BuildEmailOfLength(Email.MaxPartLength + 1);

        Result<Email> result = Email.Create(email);

        Assert.Equal(EmailErrors.TooLong, result.Error);
    }

    [Fact]
    public void Create_MaxLengthPlusSurroundingSpaces_Succeeds()
    {
        // Length is checked after trimming, so the spaces don't count.
        string email = $"   {BuildEmailOfLength(Email.MaxPartLength)}   ";

        Result<Email> result = Email.Create(email);

        Assert.True(result.IsSuccess);
    }

    // ---------- Helpers ----------

    private static string BuildEmailOfLength(int length) =>
        new string('a', length - Domain.Length) + Domain;
}
