using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// A button this bot does not know: from an older version, or its data was kept in memory and the bot restarted.
/// </summary>
public class OutdatedCommand : AbstractCommand<Message?, CallbackQuery>
{
    public OutdatedCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, CallbackQuery data)
        : base(musicSearcherClient, telegramBotClient, data)
    {
    }

    public static Task AnswerAsync(ITelegramBotClient botClient, CallbackQuery query)
    {
        return botClient.AnswerCallbackQuery(query.Id, CommonScreens.OutdatedButton, showAlert: true);
    }

    public override async Task<Message?> ExecuteAsync()
    {
        await AnswerAsync(TelegramBotClient, Data);
        Result = CommandResult.Outdated;
        AnsweredCallback = true;
        return null;
    }
}
