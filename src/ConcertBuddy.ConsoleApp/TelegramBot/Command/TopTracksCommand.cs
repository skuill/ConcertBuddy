using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// The top tracks of the artist on Last.fm in place of the card.
/// </summary>
public class TopTracksCommand : ScreenCallbackCommand
{
    private readonly TrackScreens _screens;

    public TopTracksCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, TrackScreens screens, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, sender, data, payload)
    {
        _screens = screens;
    }

    protected override int ArgumentCount => 1;

    protected override Func<Task<ScreenResult>>? Prepare(string[] arguments)
    {
        var mbid = arguments[0];
        return Guid.TryParse(mbid, out _) ? () => _screens.TopTracksAsync(mbid) : null;
    }
}
