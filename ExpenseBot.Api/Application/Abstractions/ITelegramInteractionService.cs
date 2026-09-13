namespace ExpenseBot.Api.Application.Abstractions;

public interface ITelegramInteractionService
{
    Task AnswerCallbackAsync(
        string callbackQueryId,
        string? text,
        bool showAlert,
        CancellationToken cancellationToken);

    Task DeleteMessagesAsync(
        long chatId,
        IEnumerable<int> messageIds,
        CancellationToken cancellationToken);
}
