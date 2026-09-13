using System.Globalization;
using ExpenseBot.Api.Application.Abstractions;
using ExpenseBot.Api.Application.Telegram;
using ExpenseBot.Api.Domain.Entities;
using ExpenseBot.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExpenseBot.Api.UseCases.Expenses.RecordExpense;

public sealed record RecordExpenseCommand(
    long ChatId,
    string ChatType,
    long TelegramUserId,
    string FirstName,
    int MessageId,
    int? ReplyToMessageId,
    string Text,
    DateTime MessageDateUtc) : IRequest;

public sealed class RecordExpenseCommandHandler(
    IDbContextFactory<ExpenseBotDbContext> dbContextFactory,
    ITelegramBotClient botClient,
    ITelegramInteractionService telegramInteractionService,
    ILogger<RecordExpenseCommandHandler> logger)
    : IRequestHandler<RecordExpenseCommand>
{
    private static readonly TimeSpan DraftLifetime = TimeSpan.FromMinutes(30);

    public async Task Handle(RecordExpenseCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var state = await (
                from draft in dbContext.ExpenseDrafts
                join member in dbContext.LedgerMembers
                    on new { draft.LedgerId, draft.UserId } equals new { member.LedgerId, member.UserId }
                join ledger in dbContext.Ledgers on member.LedgerId equals ledger.Id
                join user in dbContext.Users on member.UserId equals user.Id
                join category in dbContext.Categories
                    on new { draft.CategoryId, draft.LedgerId }
                    equals new { CategoryId = (long?)category.Id, category.LedgerId }
                where ledger.TelegramChatId == request.ChatId &&
                      user.TelegramUserId == request.TelegramUserId &&
                      draft.ExpiresAtUtc > DateTime.UtcNow
                select new
                {
                    Draft = draft,
                    Ledger = ledger,
                    CategoryName = category.Name
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (state?.Draft.PromptMessageId is null)
        {
            return;
        }

        if (request.ChatType != "private" && request.ReplyToMessageId != state.Draft.PromptMessageId)
        {
            return;
        }

        if (!ExpenseInputParser.TryParse(request.Text, out var input) || input is null)
        {
            var retryPrompt = await botClient.SendMessage(
                chatId: request.ChatId,
                text: $"{request.FirstName}, enter a positive amount and, optionally, a description after it. " +
                      "For example: 89.30 lunch.",
                replyParameters: new ReplyParameters { MessageId = request.MessageId },
                replyMarkup: new ForceReplyMarkup
                {
                    Selective = true,
                    InputFieldPlaceholder = "For example: 89.30 lunch"
                },
                cancellationToken: cancellationToken);

            state.Draft.PromptMessageId = retryPrompt.MessageId;
            state.Draft.FlowMessageIds = state.Draft.FlowMessageIds
                .Append(request.MessageId)
                .Append(retryPrompt.MessageId)
                .Distinct()
                .ToArray();
            state.Draft.ExpiresAtUtc = DateTime.UtcNow.Add(DraftLifetime);
            state.Draft.UpdatedAtUtc = DateTime.UtcNow;

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await telegramInteractionService.DeleteMessagesAsync(
                    request.ChatId,
                    [retryPrompt.MessageId],
                    cancellationToken);
            }

            return;
        }

        var expense = new Expense
        {
            LedgerId = state.Draft.LedgerId,
            CreatedByUserId = state.Draft.UserId,
            CategoryId = state.Draft.CategoryId!.Value,
            Amount = input.Amount,
            Currency = state.Ledger.DefaultCurrency,
            Description = input.Description,
            RawText = request.Text,
            TelegramMessageId = request.MessageId,
            OccurredAtUtc = request.MessageDateUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Expenses.Add(expense);
        dbContext.ExpenseDrafts.Remove(state.Draft);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation(
                "Concurrent expense submission ignored. ChatId: {ChatId}, UserId: {TelegramUserId}",
                request.ChatId,
                request.TelegramUserId);
            return;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            logger.LogInformation(
                "Duplicate expense message ignored. ChatId: {ChatId}, MessageId: {MessageId}",
                request.ChatId,
                request.MessageId);
            return;
        }

        await telegramInteractionService.DeleteMessagesAsync(
            request.ChatId,
            state.Draft.FlowMessageIds.Append(request.MessageId),
            cancellationToken);

        await botClient.SendMessage(
            chatId: request.ChatId,
            text: $"✅ {request.FirstName} added {input.Amount.ToString("F2", CultureInfo.InvariantCulture)} " +
                  $"{state.Ledger.DefaultCurrency} · {state.CategoryName}" +
                  (input.Description is null ? string.Empty : $" · {input.Description}"),
            cancellationToken: cancellationToken);
    }
}
