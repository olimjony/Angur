using Angur.Domain.Abstractions;
using Angur.Domain.Accounts;

namespace Angur.Domain.UnitTests.Accounts;

public class AccountNumberTests
{
    private const string ValidNumber = "20206972000000012345";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhitespace_ReturnsRequired(string? value)
    {
        Result<AccountNumber> result = AccountNumber.Create(value);

        Assert.Equal(AccountNumberErrors.Required, result.Error);
    }

    [Theory]
    [InlineData("2020697200000001234")]     // 19 digits
    [InlineData("202069720000000123456")]   // 21 digits
    [InlineData("2020 6972 0000 0001 2345")] // spaces inside are not allowed
    public void Create_WrongLength_ReturnsInvalidLength(string value)
    {
        Result<AccountNumber> result = AccountNumber.Create(value);

        Assert.Equal(AccountNumberErrors.InvalidLength, result.Error);
    }

    [Theory]
    [InlineData("2020697200000001234A")]    // letter
    [InlineData("2020697200000001234-")]    // symbol
    [InlineData("2020697200000001234٣")]    // Arabic-Indic digit: char.IsDigit says yes, IsAsciiDigit says no
    public void Create_NonDigitCharacters_ReturnsInvalidFormat(string value)
    {
        Result<AccountNumber> result = AccountNumber.Create(value);

        Assert.Equal(AccountNumberErrors.InvalidFormat, result.Error);
    }

    [Theory]
    [InlineData(ValidNumber)]
    [InlineData("  20206972000000012345  ")]
    [InlineData("00000000000000000001")]     // leading zeros are kept (that's why it's a string)
    public void Create_TwentyDigits_ReturnsTrimmedNumber(string value)
    {
        Result<AccountNumber> result = AccountNumber.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value.Trim(), result.Value.Value);
    }

    [Fact]
    public void ToString_ReturnsNumber()
    {
        Assert.Equal(ValidNumber, AccountNumber.Create(ValidNumber).Value.ToString());
    }

    [Fact]
    public void Equals_SameNumber_AreEqual()
    {
        Assert.Equal(AccountNumber.Create(ValidNumber).Value, AccountNumber.Create($" {ValidNumber} ").Value);
    }
}
