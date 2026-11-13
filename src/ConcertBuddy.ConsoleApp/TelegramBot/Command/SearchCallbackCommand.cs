using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// Pages of the search, "Try again", the example artists and "Results" of the artist card.
/// </summary>
public class SearchCallbackCommand : ScreenCallbackCommand
{
    private readonly SearchScreens _screens;
    private int _offset;
    private string _text = string.Empty;

    public SearchCallbackCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, SearchScreens screens, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, sender, data, payload)
    {
        _screens = screens;
    }

    protected override int ArgumentCount => 2;

    // A new search (an example, "Try again") shows that it is working; a page turn just changes the list.
    protected override Screen? Waiting => _offset == 0 ? SearchScreens.Searching(_text) : null;

    protected override Func<Task<ScreenResult>>? Prepare(string[] arguments)
    {
        if (!int.TryParse(arguments[0], NumberStyles.None, CultureInfo.InvariantCulture, out _offset) || arguments[1].Length == 0)
        {
            return null;
        }

        _text = arguments[1];
        return () => _screens.SearchAsync(_text, _offset);
    }
}
