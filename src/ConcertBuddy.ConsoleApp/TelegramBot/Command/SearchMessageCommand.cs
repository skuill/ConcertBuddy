using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class SearchMessageCommand : AbstractCommand<Message?, Message>
    {
        private const string CurrentCommand = CommandList.COMMAND_SEARCH;

        private ILogger<SearchMessageCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<SearchMessageCommand>>();

        public SearchMessageCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, Message data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message?> ExecuteAsync()
        {
            _logger?.LogDebug($"Handle [{CurrentCommand}] message command: [{Data.Text}]");

            if (Data == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Unexpected case. [Data] field is null.");
                return null;
            }

            string replyText = string.Empty;
            string artistName = Data.GetClearMessage();

            var artists = await SearchHandler.SearchArtistsByName(artistName);

            if (artists == null || !artists.Any())
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find artist [{artistName}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Chat.Id);
            }

            if (artists.Count() == 1)
            {
                // TODO return artist immediately
                //Data.Data = artists.First().Name;
                //return await new ArtistCommand(SearchHandler, TelegramBotClient, artistName).Execute();
            }

            bool isForwardNavigationEnabled = artists.Count() == SearchConstants.SEARCH_ARTISTS_LIMIT_DEFAULT;
            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetArtistsInlineKeyboard(artists)
                .WithNavigationButtons(CommandList.CALLBACK_DATA_FORMAT_SEARCH, artistName, 0, SearchConstants.SEARCH_ARTISTS_LIMIT_DEFAULT, isForwardNavigationEnabled);

            replyText = "Choose the correct artist 💭:";

            return await TelegramBotClient.SendMessage(
                chatId: Data.Chat.Id,
                text: replyText,
                replyMarkup: inlineKeyboard);
        }
    }
}
