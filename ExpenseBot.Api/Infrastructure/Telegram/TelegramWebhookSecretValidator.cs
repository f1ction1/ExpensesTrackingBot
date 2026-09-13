using System.Security.Cryptography;
using System.Text;

namespace ExpenseBot.Api.Infrastructure.Telegram;

public static class TelegramWebhookSecretValidator
{
    public const string HeaderName = "X-Telegram-Bot-Api-Secret-Token";

    public static bool IsValid(string configuredSecret, string? suppliedSecret)
    {
        if (string.IsNullOrEmpty(configuredSecret))
        {
            return true;
        }

        if (string.IsNullOrEmpty(suppliedSecret))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(configuredSecret);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedSecret);

        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
