using ExpenseBot.Api.Application.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace ExpenseBot.Api.Infrastructure.Telegram;

public sealed class TelegramInteractionService(
    ITelegramBotClient botClient,
    ILogger<TelegramInteractionService> logger) : ITelegramInteractionService
{
    public async Task AnswerCallbackAsync(
        string callbackQueryId,
        string? text,
        bool showAlert,
        CancellationToken cancellationToken)
    {
        try
        {
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQueryId,
                text: text,
                showAlert: showAlert,
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException exception) when (IsExpiredCallbackQuery(exception))
        {
            logger.LogWarning(
                "Telegram callback query {CallbackQueryId} was already answered or expired: {ErrorMessage}",
                callbackQueryId,
                exception.Message);
        }
    }

    public async Task DeleteMessagesAsync(
        long chatId,
        IEnumerable<int> messageIds,
        CancellationToken cancellationToken)
    {
        foreach (var messageId in messageIds.Distinct())
        {
            try
            {
                await botClient.DeleteMessage(
                    chatId: chatId,
                    messageId: messageId,
                    cancellationToken: cancellationToken);
            }
            catch (ApiRequestException exception) when (exception.ErrorCode is 400 or 403)
            {
                logger.LogWarning(
                    "Could not delete Telegram message {MessageId} in chat {ChatId}: {ErrorMessage}",
                    messageId,
                    chatId,
                    exception.Message);
            }
        }
    }

    private static bool IsExpiredCallbackQuery(ApiRequestException exception) =>
        exception.ErrorCode == 400 &&
        (exception.Message.Contains("query is too old", StringComparison.OrdinalIgnoreCase) ||
         exception.Message.Contains("query ID is invalid", StringComparison.OrdinalIgnoreCase));
}
