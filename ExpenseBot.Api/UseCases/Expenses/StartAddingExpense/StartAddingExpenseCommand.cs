using ExpenseBot.Api.Application.Abstractions;
using ExpenseBot.Api.Application.Telegram;
using ExpenseBot.Api.Domain.Entities;
using ExpenseBot.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExpenseBot.Api.UseCases.Expenses.StartAddingExpense;

public sealed record StartAddingExpenseCommand(
    long ChatId,
    string ChatType,
    string ChatName,
    int RequestMessageId,
    long TelegramUserId,
    string FirstName,
    string? Username) : IRequest;

public sealed class StartAddingExpenseCommandHandler(
    ITelegramContextService telegramContextService,
    IDbContextFactory<ExpenseBotDbContext> dbContextFactory,
    ITelegramBotClient botClient,
    ITelegramInteractionService telegramInteractionService)
    : IRequestHandler<StartAddingExpenseCommand>
{
    private static readonly TimeSpan DraftLifetime = TimeSpan.FromMinutes(30);

    public async Task Handle(StartAddingExpenseCommand request, CancellationToken cancellationToken)
    {
        var context = await telegramContextService.GetOrCreateAsync(
            new TelegramActor(request.TelegramUserId, request.FirstName, request.Username),
            new TelegramChatDescriptor(request.ChatId, request.ChatType, request.ChatName),
            cancellationToken);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.LedgerId == context.LedgerId)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new CategoryButton(category.Id, category.Name, category.Emoji))
            .ToListAsync(cancellationToken);

        var existingDraft = await dbContext.ExpenseDrafts.SingleOrDefaultAsync(
            draft => draft.LedgerId == context.LedgerId && draft.UserId == context.UserId,
            cancellationToken);

        if (existingDraft?.FlowMessageIds.Contains(request.RequestMessageId) == true)
        {
            return;
        }

        var previousMessageIds = existingDraft?.FlowMessageIds ?? [];
        var categoryMenu = await botClient.SendMessage(
            chatId: request.ChatId,
            text: $"{request.FirstName}, choose an expense category:",
            replyMarkup: BuildKeyboard(categories, request.TelegramUserId, request.RequestMessageId),
            cancellationToken: cancellationToken);

        var now = DateTime.UtcNow;

        if (existingDraft is null)
        {
            dbContext.ExpenseDrafts.Add(new ExpenseDraft
            {
                LedgerId = context.LedgerId,
                UserId = context.UserId,
                FlowMessageIds = [request.RequestMessageId, categoryMenu.MessageId],
                ExpiresAtUtc = now.Add(DraftLifetime),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            existingDraft.CategoryId = null;
            existingDraft.PromptMessageId = null;
            existingDraft.FlowMessageIds = [request.RequestMessageId, categoryMenu.MessageId];
            existingDraft.ExpiresAtUtc = now.Add(DraftLifetime);
            existingDraft.UpdatedAtUtc = now;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await telegramInteractionService.DeleteMessagesAsync(
                request.ChatId,
                [request.RequestMessageId, categoryMenu.MessageId],
                cancellationToken);
            return;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await telegramInteractionService.DeleteMessagesAsync(
                request.ChatId,
                [request.RequestMessageId, categoryMenu.MessageId],
                cancellationToken);
            return;
        }
        catch
        {
            await telegramInteractionService.DeleteMessagesAsync(
                request.ChatId,
                [categoryMenu.MessageId],
                cancellationToken);
            throw;
        }

        await telegramInteractionService.DeleteMessagesAsync(
            request.ChatId,
            previousMessageIds,
            cancellationToken);
    }

    private static InlineKeyboardMarkup BuildKeyboard(
        IReadOnlyCollection<CategoryButton> categories,
        long telegramUserId,
        int requestMessageId)
    {
        var rows = categories
            .Chunk(2)
            .Select(row => row
                .Select(category => InlineKeyboardButton.WithCallbackData(
                    $"{category.Emoji} {category.Name}",
                    ExpenseCallbackData.ForCategory(telegramUserId, requestMessageId, category.Id)))
                .ToArray())
            .ToList();

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                "❌ Cancel",
                ExpenseCallbackData.ForCancellation(telegramUserId, requestMessageId))
        ]);

        return new InlineKeyboardMarkup(rows);
    }

    private sealed record CategoryButton(long Id, string Name, string Emoji);
}
