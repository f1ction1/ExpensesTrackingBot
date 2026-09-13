using ExpenseBot.Api.Application.Abstractions;
using ExpenseBot.Api.Application.Telegram;
using ExpenseBot.Api.Infrastructure.Telegram;
using ExpenseBot.Api.UseCases.Expenses.CancelAddingExpense;
using ExpenseBot.Api.UseCases.Expenses.RecordExpense;
using ExpenseBot.Api.UseCases.Expenses.SelectExpenseCategory;
using ExpenseBot.Api.UseCases.Expenses.StartAddingExpense;
using ExpenseBot.Api.UseCases.Telegram.SendWelcome;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ExpenseBot.Api.Controllers;

[ApiController]
[Route("api/telegram")]
public sealed class TelegramWebhookController(
    ISender sender,
    ITelegramInteractionService telegramInteractionService,
    ITelegramUpdateGate telegramUpdateGate,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<TelegramWebhookController> logger) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Post(
        [FromBody] Update update,
        [FromHeader(Name = TelegramWebhookSecretValidator.HeaderName)] string? webhookSecret,
        CancellationToken cancellationToken)
    {
        if (!TelegramWebhookSecretValidator.IsValid(
                telegramOptions.Value.WebhookSecret,
                webhookSecret))
        {
            return Unauthorized();
        }

        if (!await telegramUpdateGate.TryAcquireAsync(update.Id, cancellationToken))
        {
            return Ok();
        }

        try
        {
            if (update.CallbackQuery is { } callbackQuery)
            {
                await RouteCallbackAsync(callbackQuery, cancellationToken);
            }
            else if (update.Message is { Text: not null, From: not null } message)
            {
                await RouteMessageAsync(message, cancellationToken);
            }

            await telegramUpdateGate.CompleteAsync(update.Id, cancellationToken);
        }
        catch
        {
            await telegramUpdateGate.ReleaseAsync(update.Id, CancellationToken.None);
            throw;
        }

        return Ok();
    }

    private async Task RouteMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var text = message.Text!.Trim();
        var command = GetCommand(text);
        var user = message.From!;

        logger.LogInformation(
            "Telegram message received. ChatId: {ChatId}, UserId: {UserId}, Username: {Username}",
            message.Chat.Id,
            user.Id,
            user.Username);

        switch (command)
        {
            case "/start":
                await sender.Send(new SendWelcomeCommand(message.Chat.Id), cancellationToken);
                return;

            case "/add":
                await sender.Send(
                    new StartAddingExpenseCommand(
                        ChatId: message.Chat.Id,
                        ChatType: ToDatabaseChatType(message.Chat.Type),
                        ChatName: GetChatName(message),
                        RequestMessageId: message.MessageId,
                        TelegramUserId: user.Id,
                        FirstName: user.FirstName,
                        Username: user.Username),
                    cancellationToken);
                return;

            case "/cancel":
                await sender.Send(
                    new CancelAddingExpenseCommand(
                        ChatId: message.Chat.Id,
                        TelegramUserId: user.Id,
                        TriggerMessageId: message.MessageId,
                        RequestMessageId: null,
                        CallbackQueryId: null),
                    cancellationToken);
                return;
        }

        if (command is not null)
        {
            return;
        }

        await sender.Send(
            new RecordExpenseCommand(
                ChatId: message.Chat.Id,
                ChatType: ToDatabaseChatType(message.Chat.Type),
                TelegramUserId: user.Id,
                FirstName: user.FirstName,
                MessageId: message.MessageId,
                ReplyToMessageId: message.ReplyToMessage?.MessageId,
                Text: text,
                MessageDateUtc: message.Date.ToUniversalTime()),
            cancellationToken);
    }

    private async Task RouteCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.Message is null ||
            !ExpenseCallbackData.TryParse(callbackQuery.Data, out var callback) ||
            callback is null)
        {
            await telegramInteractionService.AnswerCallbackAsync(
                callbackQuery.Id,
                "Unknown action.",
                showAlert: true,
                cancellationToken);
            return;
        }

        if (callbackQuery.From.Id != callback.InitiatorTelegramUserId)
        {
            await telegramInteractionService.AnswerCallbackAsync(
                callbackQuery.Id,
                "This menu belongs to another user. Use /add to add your own expense.",
                showAlert: true,
                cancellationToken);
            return;
        }

        if (callback.Action == ExpenseCallbackAction.Cancel)
        {
            await sender.Send(
                new CancelAddingExpenseCommand(
                    ChatId: callbackQuery.Message.Chat.Id,
                    TelegramUserId: callbackQuery.From.Id,
                    TriggerMessageId: callbackQuery.Message.MessageId,
                    RequestMessageId: callback.RequestMessageId,
                    CallbackQueryId: callbackQuery.Id),
                cancellationToken);
            return;
        }

        await sender.Send(
            new SelectExpenseCategoryCommand(
                CallbackQueryId: callbackQuery.Id,
                ChatId: callbackQuery.Message.Chat.Id,
                MenuMessageId: callbackQuery.Message.MessageId,
                RequestMessageId: callback.RequestMessageId,
                TelegramUserId: callbackQuery.From.Id,
                FirstName: callbackQuery.From.FirstName,
                CategoryId: callback.CategoryId!.Value),
            cancellationToken);
    }

    private static string? GetCommand(string text)
    {
        if (!text.StartsWith('/'))
        {
            return null;
        }

        var commandWithBotName = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
        return commandWithBotName.Split('@', 2)[0].ToLowerInvariant();
    }

    private static string GetChatName(Message message) =>
        message.Chat.Type == ChatType.Private
            ? $"{message.From!.FirstName}'s expenses"
            : message.Chat.Title ?? "Telegram expenses";

    private static string ToDatabaseChatType(ChatType chatType) => chatType switch
    {
        ChatType.Private => "private",
        ChatType.Group => "group",
        ChatType.Supergroup => "supergroup",
        _ => "unsupported"
    };
}
