using ExpenseBot.Api.Application.Telegram;
using System.Text;

namespace ExpenseBot.Api.Tests;

public sealed class ExpenseCallbackDataTests
{
    [Fact]
    public void CategoryCallback_RoundTrips()
    {
        var data = ExpenseCallbackData.ForCategory(123456789, 42, 7);

        var parsed = ExpenseCallbackData.TryParse(data, out var callback);

        Assert.True(parsed);
        Assert.NotNull(callback);
        Assert.Equal(ExpenseCallbackAction.SelectCategory, callback.Action);
        Assert.Equal(123456789, callback.InitiatorTelegramUserId);
        Assert.Equal(42, callback.RequestMessageId);
        Assert.Equal(7, callback.CategoryId);
    }

    [Fact]
    public void CancellationCallback_RoundTrips()
    {
        var data = ExpenseCallbackData.ForCancellation(123456789, 42);

        var parsed = ExpenseCallbackData.TryParse(data, out var callback);

        Assert.True(parsed);
        Assert.NotNull(callback);
        Assert.Equal(ExpenseCallbackAction.Cancel, callback.Action);
        Assert.Null(callback.CategoryId);
    }

    [Fact]
    public void CategoryCallback_WithMaximumNumericIds_FitsTelegramLimit()
    {
        var data = ExpenseCallbackData.ForCategory(long.MaxValue, int.MaxValue, long.MaxValue);

        Assert.True(Encoding.UTF8.GetByteCount(data) <= 64);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown:1:2")]
    [InlineData("ec:1:2")]
    [InlineData("xc:one:2")]
    public void TryParse_MalformedCallback_ReturnsFalse(string? data)
    {
        Assert.False(ExpenseCallbackData.TryParse(data, out _));
    }
}
