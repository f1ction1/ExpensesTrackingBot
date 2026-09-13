using System.Globalization;
using System.Text.RegularExpressions;

namespace ExpenseBot.Api.Application.Telegram;

public sealed record ParsedExpenseInput(decimal Amount, string? Description);

public static partial class ExpenseInputParser
{
    public const int MaxDescriptionLength = 500;

    [GeneratedRegex(
        @"^(?<amount>(?:\d{1,3}(?:[ \u00A0]\d{3})+|\d+)(?:[.,]\d{1,2})?)(?:\s+(?<description>.*))?$",
        RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex ExpenseInputPattern();

    public static bool TryParse(string text, out ParsedExpenseInput? input)
    {
        input = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var match = ExpenseInputPattern().Match(text.Trim());
        if (!match.Success)
        {
            return false;
        }

        var normalizedAmount = match.Groups["amount"].Value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace(',', '.');

        if (!decimal.TryParse(
                normalizedAmount,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount) ||
            amount <= 0 ||
            amount > 999_999_999_999.99m ||
            decimal.Round(amount, 2) != amount)
        {
            return false;
        }

        var descriptionGroup = match.Groups["description"];
        var description = descriptionGroup.Success ? descriptionGroup.Value.Trim() : null;

        if (description is { Length: > MaxDescriptionLength })
        {
            return false;
        }

        input = new ParsedExpenseInput(amount, description);
        return true;
    }
}
