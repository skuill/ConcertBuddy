using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class SearchCommand : AbstractCommand<Message, Message>
    {
        private ILogger<SearchCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<SearchCommand>>();

        public SearchCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, Message data) : base(searchHandler, telegramBotClient, data)
        {
        }

        public async Task<Message> Execute()
        {
            _logger.LogDebug($"Handle search command: [{Data.Text}]");

            var split_message = Data.GetSplitMessageText();
            string replyText = string.Empty;

            if (split_message.Count() == 1)
            {
                replyText = $"Please pass artist's name as a parameter. For example: [{CommandList.COMMAND_SEARCH} The Beatles]";
                return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            var artistName = Data.GetParameterFromMessageText(CommandList.COMMAND_SEARCH);

            var artists = await SearchHandler.SearchArtistsByName(artistName);

            if (artists == null || !artists.Any())
            {
                _logger.LogError($"Can't find artist [{artistName}]");

                replyText = "Something goes wrong :(! Please try to find another artist. ";
                return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            if (artists.Count() == 1)
            {
                // TODO: answer immediately without inlineKeyboardButtons choice.
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetArtistsWithScoreInlineKeyboard(artists);

            replyText = "Choose the right artist:";
            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Chat.Id,
                                                        text: replyText,
                                                        replyMarkup: inlineKeyboard);
        }
    }
}
