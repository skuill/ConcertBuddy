using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// The artist card in place of the search results.
/// </summary>
public class ArtistCommand : ScreenCallbackCommand
{
    private readonly ArtistScreens _screens;

    public ArtistCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, ArtistScreens screens, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, sender, data, payload)
    {
        _screens = screens;
    }

    protected override int ArgumentCount => 3;

    protected override Func<Task<ScreenResult>>? Prepare(string[] arguments)
    {
        var mbid = arguments[0];
        if (!Guid.TryParse(mbid, out _) || !int.TryParse(arguments[1], NumberStyles.None, CultureInfo.InvariantCulture, out var offset))
        {
            return null;
        }

        return () => _screens.CardAsync(mbid, offset, arguments[2]);
    }
}
