using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// /start, /help, /about and "/search" without a name: a new message with the screen.
/// </summary>
public class InfoCommand : AbstractCommand<Message?, Message>
{
    private readonly ScreenSender _sender;
    private readonly InfoScreens _screens;
    private readonly InfoPage _page;

    public InfoCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, InfoScreens screens, Message data, InfoPage page)
        : base(musicSearcherClient, telegramBotClient, data)
    {
        _sender = sender;
        _screens = screens;
        _page = page;
    }

    public override async Task<Message?> ExecuteAsync()
    {
        var screen = _page switch
        {
            InfoPage.Start => _screens.Start(Data.From?.FirstName),
            InfoPage.Help => _screens.Help(),
            InfoPage.About => _screens.About(),
            _ => InfoScreens.SearchHint(),
        };
        return await _sender.SendAsync(Data.Chat.Id, screen);
    }
}
