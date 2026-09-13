namespace ExpenseBot.Api.Domain;

public sealed record DefaultExpenseCategory(string Name, string NormalizedName, string Emoji, int SortOrder);

public static class DefaultExpenseCategories
{
    public static readonly IReadOnlyList<DefaultExpenseCategory> All =
    [
        new("Groceries", "groceries", "🛒", 10),
        new("Transport", "transport", "🚕", 20),
        new("Housing", "housing", "🏠", 30),
        new("Entertainment", "entertainment", "🎬", 40),
        new("Health", "health", "💊", 50),
        new("Other", "other", "📦", 60)
    ];
}
