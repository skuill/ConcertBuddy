using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

public class SearchMessageCommand : AbstractCommand<Message?, Message>
{
    private const string CurrentCommand = CommandList.COMMAND_SEARCH;

    private readonly ILogger<SearchMessageCommand> _logger;

    public SearchMessageCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ILogger<SearchMessageCommand> logger, Message data)
        : base(musicSearcherClient, telegramBotClient, data)
    {
        _logger = logger;
    }

    public override async Task<Message?> ExecuteAsync()
    {
        _logger?.LogDebug("Handle [{CurrentCommand}] message command: [{Text}]", CurrentCommand, Data.Text);

        if (Data == null)
        {
            _logger?.LogError("Command: [{CurrentCommand}]. Unexpected case. [Data] field is null.", CurrentCommand);
            return null;
        }

        string replyText = string.Empty;
        string artistName = Data.GetClearMessage();

        var artists = await MusicSearcherClient.SearchArtistsByName(artistName);

        if (artists == null || !artists.Any())
        {
            _logger?.LogInformation("Command: [{CurrentCommand}]. Can't find artist [{ArtistName}]", CurrentCommand, artistName);
            return await SendNotFoundAsync(Data.Chat.Id);
        }

        if (artists.Count() == 1)
        {
            // TODO return artist immediately
            //Data.Data = artists.First().Name;
            //return await new ArtistCommand(MusicSearcherClient, TelegramBotClient, artistName).Execute();
        }

        bool isForwardNavigationEnabled = artists.Count() == SearchConstants.SEARCH_ARTISTS_LIMIT_DEFAULT;
        InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetArtistsInlineKeyboard(artists)
            .WithNavigationButtons(
                CommandList.CALLBACK_DATA_FORMAT_SEARCH,
                artistName,
                0,
                limit: SearchConstants.SEARCH_ARTISTS_LIMIT_DEFAULT,
                isForwardNavigationEnabled: isForwardNavigationEnabled);

        replyText = "Choose the correct artist 💭:";

        return await TelegramBotClient.SendMessage(
            chatId: Data.Chat.Id,
            text: replyText,
            replyMarkup: inlineKeyboard);
    }
}
