using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler;

public interface IBotHandlers
{
    Task HandleUpdateAsync(Update update, CancellationToken cancellationToken);
}
