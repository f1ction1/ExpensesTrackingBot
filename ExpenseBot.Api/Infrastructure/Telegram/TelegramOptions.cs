namespace ExpenseBot.Api.Infrastructure.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string Token { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
}
