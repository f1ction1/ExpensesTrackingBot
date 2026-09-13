using ExpenseBot.Api.Application.Abstractions;
using ExpenseBot.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace ExpenseBot.Api.UseCases.Expenses.CancelAddingExpense;

public sealed record CancelAddingExpenseCommand(
    long ChatId,
    long TelegramUserId,
    int TriggerMessageId,
    int? RequestMessageId,
    string? CallbackQueryId) : IRequest;

public sealed class CancelAddingExpenseCommandHandler(
    IDbContextFactory<ExpenseBotDbContext> dbContextFactory,
    ITelegramBotClient botClient,
    ITelegramInteractionService telegramInteractionService)
    : IRequestHandler<CancelAddingExpenseCommand>
{
    public async Task Handle(CancelAddingExpenseCommand request, CancellationToken cancellationToken)
    {
        if (request.CallbackQueryId is not null)
        {
            await telegramInteractionService.AnswerCallbackAsync(
                request.CallbackQueryId,
                text: "Cancelled",
                showAlert: false,
                cancellationToken);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await dbContext.ExpenseDrafts
            .Where(candidate =>
                candidate.Member.Ledger.TelegramChatId == request.ChatId &&
                candidate.Member.User.TelegramUserId == request.TelegramUserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (draft is null)
        {
            if (request.CallbackQueryId is not null)
            {
                await telegramInteractionService.DeleteMessagesAsync(
                    request.ChatId,
                    new int?[] { request.RequestMessageId, request.TriggerMessageId }
                        .Where(messageId => messageId.HasValue)
                        .Select(messageId => messageId!.Value),
                    cancellationToken);
            }
            else
            {
                await botClient.SendMessage(
                    chatId: request.ChatId,
                    text: "You do not have a pending expense.",
                    cancellationToken: cancellationToken);
            }

            return;
        }

        var messageIds = draft.FlowMessageIds.Append(request.TriggerMessageId).Distinct().ToArray();
        dbContext.ExpenseDrafts.Remove(draft);
        await dbContext.SaveChangesAsync(cancellationToken);

        await telegramInteractionService.DeleteMessagesAsync(
            request.ChatId,
            messageIds,
            cancellationToken);
    }
}
