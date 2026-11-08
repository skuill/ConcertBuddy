using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

public class UsageCommand : AbstractCommand<Message?, Message>
{
    private readonly ILogger<UsageCommand> _logger;

    public UsageCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ILogger<UsageCommand> logger, Message data)
        : base(musicSearcherClient, telegramBotClient, data)
    {
        _logger = logger;
    }

    public override async Task<Message?> ExecuteAsync()
    {
        string usage = $"Hi, {Data.From?.FirstName}! 👋\n" +
            $"Please, write any artist or band name and I will find! 🔍";

        return await TelegramBotClient.SendMessage(
            chatId: Data.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove());
    }
}
