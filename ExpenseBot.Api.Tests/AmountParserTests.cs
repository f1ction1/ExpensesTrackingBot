using ExpenseBot.Api.Application.Telegram;

namespace ExpenseBot.Api.Tests;

public sealed class ExpenseInputParserTests
{
    [Theory]
    [InlineData("89.30", "89.30", null)]
    [InlineData("89,30", "89.30", null)]
    [InlineData("89,30 lunch", "89.30", "lunch")]
    [InlineData("1 234,56 weekly groceries", "1234.56", "weekly groceries")]
    [InlineData("  25.00   taxi home  ", "25.00", "taxi home")]
    public void TryParse_ValidInput_ReturnsAmountAndOptionalDescription(
        string text,
        string expectedAmount,
        string? expectedDescription)
    {
        var parsed = ExpenseInputParser.TryParse(text, out var input);

        Assert.True(parsed);
        Assert.NotNull(input);
        Assert.Equal(
            decimal.Parse(expectedAmount, System.Globalization.CultureInfo.InvariantCulture),
            input.Amount);
        Assert.Equal(expectedDescription, input.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("12.345")]
    [InlineData("9999999999999")]
    [InlineData("PLN 10")]
    [InlineData("10lunch")]
    public void TryParse_InvalidInput_ReturnsFalse(string input)
    {
        Assert.False(ExpenseInputParser.TryParse(input, out _));
    }

    [Fact]
    public void TryParse_DescriptionOverMaximumLength_ReturnsFalse()
    {
        var text = $"10 {new string('x', ExpenseInputParser.MaxDescriptionLength + 1)}";

        Assert.False(ExpenseInputParser.TryParse(text, out _));
    }
}
