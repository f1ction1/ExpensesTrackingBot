namespace ExpenseBot.Api.Application.Telegram;

public sealed record ExpenseCallback(
    ExpenseCallbackAction Action,
    long InitiatorTelegramUserId,
    int RequestMessageId,
    long? CategoryId);

public enum ExpenseCallbackAction
{
    SelectCategory,
    Cancel
}

public static class ExpenseCallbackData
{
    private const string CategoryPrefix = "ec";
    private const string CancelPrefix = "xc";

    public static string ForCategory(long telegramUserId, int requestMessageId, long categoryId) =>
        $"{CategoryPrefix}:{telegramUserId}:{requestMessageId}:{categoryId}";

    public static string ForCancellation(long telegramUserId, int requestMessageId) =>
        $"{CancelPrefix}:{telegramUserId}:{requestMessageId}";

    public static bool TryParse(string? data, out ExpenseCallback? callback)
    {
        callback = null;

        if (string.IsNullOrWhiteSpace(data))
        {
            return false;
        }

        var parts = data.Split(':');

        if (parts.Length < 3 ||
            !long.TryParse(parts[1], out var userId) ||
            !int.TryParse(parts[2], out var requestMessageId))
        {
            return false;
        }

        if (parts[0] == CancelPrefix && parts.Length == 3)
        {
            callback = new ExpenseCallback(ExpenseCallbackAction.Cancel, userId, requestMessageId, null);
            return true;
        }

        if (parts[0] == CategoryPrefix &&
            parts.Length == 4 &&
            long.TryParse(parts[3], out var categoryId))
        {
            callback = new ExpenseCallback(
                ExpenseCallbackAction.SelectCategory,
                userId,
                requestMessageId,
                categoryId);
            return true;
        }

        return false;
    }
}
