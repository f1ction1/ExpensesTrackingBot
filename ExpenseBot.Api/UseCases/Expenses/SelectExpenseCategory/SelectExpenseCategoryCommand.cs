using ExpenseBot.Api.Application.Abstractions;
using ExpenseBot.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExpenseBot.Api.UseCases.Expenses.SelectExpenseCategory;

public sealed record SelectExpenseCategoryCommand(
    string CallbackQueryId,
    long ChatId,
    int MenuMessageId,
    int RequestMessageId,
    long TelegramUserId,
    string FirstName,
    long CategoryId) : IRequest;

public sealed class SelectExpenseCategoryCommandHandler(
    IDbContextFactory<ExpenseBotDbContext> dbContextFactory,
    ITelegramBotClient botClient,
    ITelegramInteractionService telegramInteractionService)
    : IRequestHandler<SelectExpenseCategoryCommand>
{
    private static readonly TimeSpan DraftLifetime = TimeSpan.FromMinutes(30);

    public async Task Handle(SelectExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        await telegramInteractionService.AnswerCallbackAsync(
            request.CallbackQueryId,
            text: null,
            showAlert: false,
            cancellationToken);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var selection = await (
                from draft in dbContext.ExpenseDrafts
                join member in dbContext.LedgerMembers
                    on new { draft.LedgerId, draft.UserId } equals new { member.LedgerId, member.UserId }
                join ledger in dbContext.Ledgers on member.LedgerId equals ledger.Id
                join user in dbContext.Users on member.UserId equals user.Id
                join category in dbContext.Categories
                    on new { Id = request.CategoryId, draft.LedgerId }
                    equals new { category.Id, category.LedgerId }
                where ledger.TelegramChatId == request.ChatId &&
                      user.TelegramUserId == request.TelegramUserId &&
                      draft.CategoryId == null &&
                      draft.PromptMessageId == null
                select new { Draft = draft, CategoryName = category.Name, category.Emoji })
            .SingleOrDefaultAsync(cancellationToken);

        if (selection is null ||
            !selection.Draft.FlowMessageIds.Contains(request.RequestMessageId) ||
            !selection.Draft.FlowMessageIds.Contains(request.MenuMessageId))
        {
            return;
        }

        var prompt = await botClient.SendMessage(
            chatId: request.ChatId,
            text: $"{request.FirstName}, enter the amount for “{selection.CategoryName}”. " +
                  "You can add a description after it, for example: 89.30 lunch.",
            replyParameters: new ReplyParameters { MessageId = request.RequestMessageId },
            replyMarkup: new ForceReplyMarkup
            {
                Selective = true,
                InputFieldPlaceholder = "For example: 89.30 lunch"
            },
            cancellationToken: cancellationToken);

        selection.Draft.CategoryId = request.CategoryId;
        selection.Draft.PromptMessageId = prompt.MessageId;
        selection.Draft.FlowMessageIds = selection.Draft.FlowMessageIds
            .Append(prompt.MessageId)
            .Distinct()
            .ToArray();
        selection.Draft.ExpiresAtUtc = DateTime.UtcNow.Add(DraftLifetime);
        selection.Draft.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await telegramInteractionService.DeleteMessagesAsync(
                request.ChatId,
                [prompt.MessageId],
                cancellationToken);
            return;
        }
        catch
        {
            await telegramInteractionService.DeleteMessagesAsync(
                request.ChatId,
                [prompt.MessageId],
                cancellationToken);
            throw;
        }

        await botClient.EditMessageText(
            chatId: request.ChatId,
            messageId: request.MenuMessageId,
            text: $"{request.FirstName} selected: {selection.Emoji} {selection.CategoryName}",
            cancellationToken: cancellationToken);
    }
}
