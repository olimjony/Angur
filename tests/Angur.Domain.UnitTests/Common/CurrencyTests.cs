using Angur.Domain.Abstractions;
using Angur.Domain.Common;

namespace Angur.Domain.UnitTests.Common;

public class CurrencyTests
{

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void FromCode_NullOrWhitespace_ReturnsEmptyError(string? code)
    {
        // Act
        Result<Currency> result = Currency.FromCode(code);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(CurrencyErrors.Empty, result.Error);
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("XYZ")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U SD")]
    public void FromCode_UnsupportedCode_ReturnsUnsupportedError(string code)
    {
        // Act
        Result<Currency> result = Currency.FromCode(code);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(CurrencyErrors.Unsupported, result.Error);
    }

    [Theory]
    [InlineData("TJS")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("RUB")]
    public void FromCode_SupportedCode_ReturnsCurrencyWithThatCode(string code)
    {
        // Act
        Result<Currency> result = Currency.FromCode(code);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(code, result.Value.Code);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData(" USD ")]
    [InlineData("usd\t")]
    public void FromCode_DifferentCaseOrSurroundingWhitespace_ReturnsSameInstance(string code)
    {
        // Act
        Result<Currency> result = Currency.FromCode(code);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Same(Currency.USD, result.Value);
    }

    // ---------- Currency data ----------

    [Theory]
    [InlineData("TJS", 2)]
    [InlineData("USD", 2)]
    [InlineData("EUR", 2)]
    [InlineData("RUB", 2)]
    public void MinorUnits_ForSupportedCurrency_MatchesIso4217(string code, int expectedMinorUnits)
    {
        // Act
        Currency currency = Currency.FromCode(code).Value;

        // Assert
        Assert.Equal(expectedMinorUnits, currency.MinorUnits);
    }

    [Fact]
    public void ToString_ReturnsCode()
    {
        Assert.Equal("TJS", Currency.TJS.ToString());
    }

    [Fact]
    public void All_ContainsExactlyTheSupportedCurrencies()
    {
        // Act
        IReadOnlyCollection<Currency> all = Currency.All;

        // Assert
        Assert.Equal(4, all.Count);
        Assert.Contains(Currency.TJS, all);
        Assert.Contains(Currency.USD, all);
        Assert.Contains(Currency.EUR, all);
        Assert.Contains(Currency.RUB, all);
    }

    [Fact]
    public void All_EveryCurrency_CanBeFoundByItsCode()
    {
        Assert.All(Currency.All, currency =>
        {
            Result<Currency> result = Currency.FromCode(currency.Code);

            Assert.True(result.IsSuccess);
            Assert.Same(currency, result.Value);
        });
    }
}
