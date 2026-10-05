using System.Globalization;

using Angur.Domain.Abstractions;
using Angur.Domain.Common;

namespace Angur.Domain.UnitTests.Common;

public class MoneyTests
{
    // ---------- Create: valid amounts ----------
    public static TheoryData<decimal> ValidTjsAmounts =>
    [
        100m,
        100.5m,
        100.50m,
        100.500m,
        0m,
        -50.25m,
    ];

    [Theory]
    [MemberData(nameof(ValidTjsAmounts))]
    public void Create_AmountWithinCurrencyScale_ReturnsSuccess(decimal amount)
    {
        // Act
        Result<Money> result = Money.Create(amount, Currency.TJS);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(amount, result.Value.Amount);
        Assert.Same(Currency.TJS, result.Value.Currency);
    }

    // ---------- Create: invalid amounts ----------

    public static TheoryData<decimal> TooPreciseTjsAmounts =>
    [
        100.125m,
        0.001m,
        1.999m,
        -0.005m,
    ];

    [Theory]
    [MemberData(nameof(TooPreciseTjsAmounts))]
    public void Create_MoreDecimalPlacesThanCurrencyAllows_ReturnsInvalidScale(decimal amount)
    {
        // Act
        Result<Money> result = Money.Create(amount, Currency.TJS);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(MoneyErrors.InvalidScale, result.Error);
    }

    [Fact]
    public void Create_NullCurrency_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Money.Create(10m, null!));
    }


    [Fact]
    public void Zero_ReturnsZeroAmountInGivenCurrency()
    {
        // Act
        Money zero = Money.Zero(Currency.USD);

        // Assert
        Assert.Equal(0m, zero.Amount);
        Assert.Same(Currency.USD, zero.Currency);
        Assert.True(zero.IsZero);
    }

    [Fact]
    public void Zero_NullCurrency_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Money.Zero(null!));
    }

    public static TheoryData<decimal, bool, bool, bool> SignCases => new()
    {
        // amount,  isZero, isPositive, isNegative
        { 0m,       true,   false,      false },
        { 0.01m,    false,  true,       false },
        { 100m,     false,  true,       false },
        { -0.01m,   false,  false,      true  },
    };

    [Theory]
    [MemberData(nameof(SignCases))]
    public void SignProperties_ReflectAmount(decimal amount, bool isZero, bool isPositive, bool isNegative)
    {
        // Act
        Money money = Tjs(amount);

        // Assert
        Assert.Equal(isZero, money.IsZero);
        Assert.Equal(isPositive, money.IsPositive);
        Assert.Equal(isNegative, money.IsNegative);
    }

    [Fact]
    public void Addition_SameCurrency_ReturnsSumInSameCurrency()
    {
        // Act
        Money sum = Tjs(100m) + Tjs(50.50m);

        // Assert
        Assert.Equal(150.50m, sum.Amount);
        Assert.Same(Currency.TJS, sum.Currency);
    }

    [Fact]
    public void Subtraction_SameCurrency_ReturnsDifference()
    {
        // Act
        Money difference = Tjs(100m) - Tjs(30.25m);

        // Assert
        Assert.Equal(69.75m, difference.Amount);
        Assert.Same(Currency.TJS, difference.Currency);
    }

    [Fact]
    public void Subtraction_ResultBelowZero_ReturnsNegativeMoney()
    {
        // Act
        Money difference = Tjs(100m) - Tjs(150m);

        // Assert
        Assert.Equal(-50m, difference.Amount);
        Assert.True(difference.IsNegative);
    }

    [Fact]
    public void Addition_DoesNotChangeOperands()
    {
        // Arrange
        Money left = Tjs(100m);
        Money right = Tjs(50m);

        // Act
        _ = left + right;

        // Assert
        Assert.Equal(100m, left.Amount);
        Assert.Equal(50m, right.Amount);
    }

    [Fact]
    public void Addition_DifferentCurrencies_ThrowsInvalidOperationExceptionNamingBoth()
    {
        // Act
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => Tjs(100m) + Usd(100m));

        // Assert
        Assert.Contains("TJS", exception.Message, StringComparison.Ordinal);
        Assert.Contains("USD", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Subtraction_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Tjs(100m) - Usd(100m));
    }

    [Fact]
    public void Addition_NullOperand_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Tjs(100m) + null!);
    }

    // ---------- Comparison ----------

    public static TheoryData<decimal, decimal> LeftGreaterThanRight => new()
    {
        { 100m, 99.99m },
        { 0m,   -1m },
    };

    [Theory]
    [MemberData(nameof(LeftGreaterThanRight))]
    public void Comparison_LeftGreater_OperatorsAgree(decimal left, decimal right)
    {
        // Arrange
        Money a = Tjs(left);
        Money b = Tjs(right);

        // Assert
        Assert.True(a > b);
        Assert.True(a >= b);
        Assert.False(a < b);
        Assert.False(a <= b);
    }

    [Fact]
    public void Comparison_EqualAmounts_OnlyInclusiveOperatorsAreTrue()
    {
        // Arrange
        Money a = Tjs(100m);
        Money b = Tjs(100.00m);

        // Assert
        Assert.False(a > b);
        Assert.False(a < b);
        Assert.True(a >= b);
        Assert.True(a <= b);
    }

    [Fact]
    public void Comparison_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange
        Money tjs = Tjs(100m);
        Money usd = Usd(100m);

        // Assert
        Assert.Throws<InvalidOperationException>(() => tjs > usd);
        Assert.Throws<InvalidOperationException>(() => tjs < usd);
        Assert.Throws<InvalidOperationException>(() => tjs >= usd);
        Assert.Throws<InvalidOperationException>(() => tjs <= usd);
    }

    // ---------- Equality ----------

    [Fact]
    public void Equals_SameValueWrittenWithDifferentScale_AreEqual()
    {
        Assert.Equal(Tjs(10m), Tjs(10.00m));
    }

    [Fact]
    public void Equals_DifferentAmount_AreNotEqual()
    {
        Assert.NotEqual(Tjs(10m), Tjs(10.01m));
    }

    [Fact]
    public void Equals_SameAmountDifferentCurrency_AreNotEqual()
    {
        Assert.NotEqual(Tjs(10m), Usd(10m));
    }

    // ---------- ToString ----------

    [Theory]
    [InlineData("100.5", "100.50 TJS")]
    [InlineData("0", "0.00 TJS")]
    [InlineData("-50.25", "-50.25 TJS")]
    [InlineData("1234567.89", "1234567.89 TJS")]
    public void ToString_FormatsAmountWithCurrencyScaleAndCode(string amount, string expected)
    {
        // Arrange
        Money money = Tjs(decimal.Parse(amount, CultureInfo.InvariantCulture));

        // Act
        string text = money.ToString();

        // Assert
        Assert.Equal(expected, text);
    }

    [Fact]
    public void ToString_UnderRussianCulture_StillUsesDotAsDecimalSeparator()
    {
        // Arrange
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

        try
        {
            // Act
            string text = Tjs(100.5m).ToString();

            // Assert
            Assert.Equal("100.50 TJS", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    // ---------- Helpers ----------

    private static Money Tjs(decimal amount) => Money.Create(amount, Currency.TJS).Value;

    private static Money Usd(decimal amount) => Money.Create(amount, Currency.USD).Value;
}
