using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// A text message is an artist search: "Searching…" at once, then the results in the same message.
/// </summary>
public class SearchMessageCommand : AbstractCommand<Message?, Message>
{
    private readonly ScreenSender _sender;
    private readonly SearchScreens _screens;

    private readonly SearchQuery _query;

    public SearchMessageCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, SearchScreens screens, Message data, SearchQuery query)
        : base(musicSearcherClient, telegramBotClient, data)
    {
        _sender = sender;
        _screens = screens;
        _query = query;
    }

    public override async Task<Message?> ExecuteAsync()
    {
        var text = SearchScreens.CleanText(_query.Text);
        var message = await _sender.SendAsync(Data.Chat.Id, SearchScreens.Searching(text));

        var result = await _screens.SearchAsync(text, offset: 0);
        Result = result.Result;
        await _sender.EditAsync(Data.Chat.Id, message.MessageId, result.Screen);
        return message;
    }
}
