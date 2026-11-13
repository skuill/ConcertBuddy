using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// The setlists of the artist, screen by screen, in place of the card.
/// </summary>
public class SetlistsCommand : ScreenCallbackCommand
{
    private readonly SetlistScreens _screens;

    public SetlistsCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, SetlistScreens screens, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, sender, data, payload)
    {
        _screens = screens;
    }

    protected override int ArgumentCount => 2;

    protected override Func<Task<ScreenResult>>? Prepare(string[] arguments)
    {
        var mbid = arguments[0];
        if (!Guid.TryParse(mbid, out _) || !int.TryParse(arguments[1], NumberStyles.None, CultureInfo.InvariantCulture, out var screen) || screen < 1)
        {
            return null;
        }

        return () => _screens.SetlistsAsync(mbid, screen);
    }
}
