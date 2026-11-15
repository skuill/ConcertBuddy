using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// Try again of a share link that met a busy setlist.fm.
/// </summary>
public class SharedSetlistCallbackCommand : ScreenCallbackCommand
{
    private readonly SetlistScreens _screens;

    public SharedSetlistCallbackCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, SetlistScreens screens, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, sender, data, payload)
    {
        _screens = screens;
    }

    protected override int ArgumentCount => 1;

    protected override Func<Task<ScreenResult>>? Prepare(string[] arguments)
    {
        var setlistId = arguments[0];
        return ShareCommand.IsSetlistId(setlistId) ? () => _screens.SharedSetlistAsync(setlistId) : null;
    }
}
