namespace ExpenseBot.Api.Application.Abstractions;

public sealed record TelegramActor(long TelegramUserId, string FirstName, string? Username);

public sealed record TelegramChatDescriptor(long TelegramChatId, string ChatType, string Name);

public sealed record TelegramContextIds(Guid UserId, Guid LedgerId);

public interface ITelegramContextService
{
    Task<TelegramContextIds> GetOrCreateAsync(
        TelegramActor actor,
        TelegramChatDescriptor chat,
        CancellationToken cancellationToken);
}
