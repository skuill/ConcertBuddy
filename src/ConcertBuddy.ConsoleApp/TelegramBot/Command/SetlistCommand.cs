using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// One setlist as a rich message in place of the setlists.
/// </summary>
public class SetlistCommand : ScreenCallbackCommand
{
    private readonly SetlistScreens _screens;

    public SetlistCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, SetlistScreens screens, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, sender, data, payload)
    {
        _screens = screens;
    }

    protected override int ArgumentCount => 3;

    protected override Func<Task<ScreenResult>>? Prepare(string[] arguments)
    {
        var setlistId = arguments[0];
        var mbid = arguments[1];
        if (setlistId.Length == 0 || !Guid.TryParse(mbid, out _)
            || !int.TryParse(arguments[2], NumberStyles.None, CultureInfo.InvariantCulture, out var screen) || screen < 1)
        {
            return null;
        }

        return () => _screens.SetlistAsync(setlistId, mbid, screen);
    }
}
