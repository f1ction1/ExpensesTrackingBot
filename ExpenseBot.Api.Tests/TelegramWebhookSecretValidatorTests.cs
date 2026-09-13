using ExpenseBot.Api.Infrastructure.Telegram;

namespace ExpenseBot.Api.Tests;

public sealed class TelegramWebhookSecretValidatorTests
{
    [Fact]
    public void IsValid_MatchingSecret_ReturnsTrue()
    {
        Assert.True(TelegramWebhookSecretValidator.IsValid("expected-secret", "expected-secret"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-secret")]
    public void IsValid_MissingOrIncorrectSecret_ReturnsFalse(string? suppliedSecret)
    {
        Assert.False(TelegramWebhookSecretValidator.IsValid("expected-secret", suppliedSecret));
    }

    [Fact]
    public void IsValid_EmptyConfiguredSecret_AllowsDevelopmentRequest()
    {
        Assert.True(TelegramWebhookSecretValidator.IsValid(string.Empty, null));
    }
}
