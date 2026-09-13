using MediatR;
using Telegram.Bot;

namespace ExpenseBot.Api.UseCases.Telegram.SendWelcome;

public sealed record SendWelcomeCommand(long ChatId) : IRequest;

public sealed class SendWelcomeCommandHandler(ITelegramBotClient botClient)
    : IRequestHandler<SendWelcomeCommand>
{
    public async Task Handle(SendWelcomeCommand request, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: request.ChatId,
            text: "Hi! I help groups track shared expenses. Use /add to add an expense.",
            cancellationToken: cancellationToken);
    }
}
