using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// The full biography in place of the artist card.
/// </summary>
public class BiographyCommand : ScreenCallbackCommand
{
    private readonly ArtistScreens _screens;

    public BiographyCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, ArtistScreens screens, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, sender, data, payload)
    {
        _screens = screens;
    }

    protected override int ArgumentCount => 1;

    protected override Func<Task<ScreenResult>>? Prepare(string[] arguments)
    {
        var mbid = arguments[0];
        return Guid.TryParse(mbid, out _) ? () => _screens.BiographyAsync(mbid) : null;
    }
}
