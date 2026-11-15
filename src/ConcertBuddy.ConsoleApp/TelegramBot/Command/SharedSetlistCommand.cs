using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// A share link opened: /start s_&lt;setlist id&gt;. "Opening…" at once, then the setlist in the same message.
/// </summary>
public class SharedSetlistCommand : AbstractCommand<Message?, Message>
{
    private readonly ScreenSender _sender;
    private readonly SetlistScreens _screens;
    private readonly SharedSetlistId _setlist;

    public SharedSetlistCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, SetlistScreens screens, Message data, SharedSetlistId setlist)
        : base(musicSearcherClient, telegramBotClient, data)
    {
        _sender = sender;
        _screens = screens;
        _setlist = setlist;
    }

    public override async Task<Message?> ExecuteAsync()
    {
        var message = await _sender.SendAsync(Data.Chat.Id, new Screen("📝 Opening the shared setlist…", InlineKeyboardMarkup.Empty()));
        var result = await _screens.SharedSetlistAsync(_setlist.Id);
        Result = result.Result;
        await _sender.EditAsync(Data.Chat.Id, message.MessageId, result.Screen);
        return message;
    }
}
