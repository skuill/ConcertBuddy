using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// Menu, Help and About buttons: the screen in place of the current one.
/// </summary>
public class InfoCallbackCommand : ScreenCallbackCommand
{
    private readonly InfoScreens _screens;

    public InfoCallbackCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, InfoScreens screens, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, sender, data, payload)
    {
        _screens = screens;
    }

    protected override int ArgumentCount => 0;

    protected override Func<Task<ScreenResult>>? Prepare(string[] arguments)
    {
        Screen? screen = Payload.Action switch
        {
            CallbackActions.Menu => _screens.Start(Data.From.FirstName),
            CallbackActions.Help => _screens.Help(),
            CallbackActions.About => _screens.About(),
            _ => null,
        };
        return screen == null ? null : () => Task.FromResult(new ScreenResult(screen, CommandResult.Found));
    }
}
